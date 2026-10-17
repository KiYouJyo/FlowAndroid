using System.Security.Cryptography;

namespace FlowAndroid.Core.Firmware;

public sealed record FirmwareTrust(bool Verified, string Explanation, OfficialFirmware? Official = null);

public static class GoogleFirmwareVerifier
{
    /// <summary>
    /// Trust only checksums obtained from Google's HTTPS catalog with an approved download origin.
    /// User-entered digests are never sufficient for safe execution.
    /// </summary>
    public static async Task<FirmwareTrust> VerifyAsync(
        string path, GoogleImageType type, IReadOnlyList<OfficialFirmware> catalog, CancellationToken ct = default)
    {
        if (!File.Exists(path)) return new(false, "Firmware file missing.");
        var name = Path.GetFileName(path);
        var matches = catalog.Where(x =>
            x.Type == type && x.DownloadUrl.Scheme == "https" && x.DownloadUrl.Host == "dl.google.com" &&
            x.DownloadUrl.AbsolutePath.StartsWith("/dl/android/aosp/", StringComparison.Ordinal) &&
            string.Equals(x.FileName, name, StringComparison.OrdinalIgnoreCase)).ToArray();
        if (matches.Length != 1 || matches[0].Sha256.Length != 64)
            return new(false, "No unique match in the official Google checksum catalog.");
        if (!Convert.TryFromHexString(matches[0].Sha256, new byte[32], out var written) || written != 32)
            return new(false, "Invalid official SHA-256.");
        await using var stream = File.OpenRead(path);
        var actual = await SHA256.HashDataAsync(stream, ct);
        var expected = Convert.FromHexString(matches[0].Sha256);
        return CryptographicOperations.FixedTimeEquals(expected, actual)
            ? new(true, "File matches the SHA-256 published on Google's HTTPS catalog.", matches[0])
            : new(false, "Checksum mismatch: file was not accepted.");
    }
}
