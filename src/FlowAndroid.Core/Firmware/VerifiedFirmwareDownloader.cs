using System.Net;
using System.Security.Cryptography;

namespace FlowAndroid.Core.Firmware;

public sealed record FirmwareDownloadProgress(long ReceivedBytes, long? TotalBytes)
{
    public int? Percent => TotalBytes is > 0
        ? (int)Math.Clamp(ReceivedBytes * 100.0 / TotalBytes.Value, 0, 100) : null;
}

/// <summary>Download an official catalog entry into a user cache, then verify SHA-256 before publishing.</summary>
public sealed class VerifiedFirmwareDownloader
{
    private const long MaxFirmwareBytes = 12L * 1024 * 1024 * 1024; // 12 GiB hard upper bound
    private readonly HttpClient _http;
    public VerifiedFirmwareDownloader(HttpClient http) => _http = http;

    public async Task<string> DownloadAsync(
        OfficialFirmware image, string directory,
        IProgress<FirmwareDownloadProgress>? progress = null,
        CancellationToken token = default)
    {
        if (image.Type != GoogleImageType.Ota || !GuidedFirmwarePlanner.IsGoogleDownload(image.DownloadUrl))
            throw new InvalidDataException("Only official Google OTA download links are supported.");
        if (!Convert.TryFromHexString(image.Sha256, new byte[32], out var written) || written != 32)
            throw new InvalidDataException("Official SHA-256 is malformed.");
        if (!System.Text.RegularExpressions.Regex.IsMatch(image.FileName, @"^[a-zA-Z0-9._-]+\.zip$") ||
            !string.Equals(Path.GetFileName(image.DownloadUrl.AbsolutePath), image.FileName, StringComparison.Ordinal))
            throw new InvalidDataException("Untrusted or inconsistent image filename.");

        Directory.CreateDirectory(directory);
        var destination = Path.Combine(directory, image.FileName);
        if (File.Exists(destination))
        {
            var existing = await GoogleFirmwareVerifier.VerifyAsync(
                destination, GoogleImageType.Ota, [image], token).ConfigureAwait(false);
            if (existing.Verified) return destination;
            throw new InvalidDataException("A different file with the same name exists in the destination. Remove it manually.");
        }

        var temporary = destination + "." + Guid.NewGuid().ToString("N") + ".partial";
        try
        {
            // Follow a small bounded set of redirects; SHA-256 verification makes malicious
            // or accidentally changed download bytes unusable even if CDN routing changes.
            using var request = new HttpRequestMessage(HttpMethod.Get, image.DownloadUrl);
            using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token)
                .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentLength is > MaxFirmwareBytes)
                throw new InvalidDataException("OTA package exceeds the maximum supported size.");
            var size = response.Content.Headers.ContentLength;
            await using var input = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
            await using var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                1024 * 128, FileOptions.Asynchronous);
            using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            var buffer = new byte[1024 * 128];
            long total = 0;
            int read;
            while ((read = await input.ReadAsync(buffer, token).ConfigureAwait(false)) > 0)
            {
                total += read;
                if (total > MaxFirmwareBytes)
                    throw new InvalidDataException("OTA package exceeded download size limit.");
                sha.AppendData(buffer, 0, read);
                await output.WriteAsync(buffer.AsMemory(0, read), token).ConfigureAwait(false);
                progress?.Report(new(total, size));
            }
            await output.FlushAsync(token).ConfigureAwait(false);
            if (size is > 0 && total != size)
                throw new EndOfStreamException("OTA download was truncated.");
            var actual = sha.GetHashAndReset();
            if (!CryptographicOperations.FixedTimeEquals(actual, Convert.FromHexString(image.Sha256)))
                throw new InvalidDataException("Downloaded ZIP hash does not match Google's published SHA-256.");
            File.Move(temporary, destination, overwrite: false);
            progress?.Report(new(total, total));
            return destination;
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }
}
