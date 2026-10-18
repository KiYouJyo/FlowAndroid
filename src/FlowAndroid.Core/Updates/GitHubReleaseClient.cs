using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace FlowAndroid.Core.Updates;

public enum ReleaseCheckState { UpdateAvailable, UpToDate, NoPublishedRelease, UnsupportedPackage, InvalidRelease }
public sealed record ReleaseAsset(string Name, Uri Url, long Size);
public sealed record UpdateRelease(Version Version, string Tag, string Notes,
    ReleaseAsset PackageAsset, ReleaseAsset ChecksumAsset);
public sealed record ReleaseCheck(ReleaseCheckState State, UpdateRelease? Update = null, string? Detail = null);
public sealed record UpdateDownloadProgress(long BytesReceived, long? TotalBytes);

/// <summary>
/// A stable GitHub release is only actionable when its version, exact MSIX asset
/// and checksum file are unambiguous. Pre-releases and action artifacts are never used.
/// </summary>
public sealed class GitHubReleaseClient
{
    public const string LatestApi = "https://api.github.com/repos/KiYouJyo/FlowAndroid/releases/latest";
    private readonly HttpClient _client;
    private const long MaxMsixBytes = 2L * 1024 * 1024 * 1024;

    public GitHubReleaseClient(HttpClient client) => _client = client;

    public async Task<ReleaseCheck> CheckAsync(Version currentVersion, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, LatestApi);
        request.Headers.UserAgent.ParseAdd("FlowAndroid-Updater/1.0");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        using var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return new(ReleaseCheckState.NoPublishedRelease);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        var payload = await JsonSerializer.DeserializeAsync<GitHubReleasePayload>(stream, cancellationToken: ct)
            .ConfigureAwait(false);
        return Select(payload, currentVersion);
    }

    public static ReleaseCheck Select(GitHubReleasePayload? release, Version currentVersion)
    {
        if (release is null || release.Draft || release.Prerelease ||
            !Regex.IsMatch(release.TagName ?? "", @"^v?\d+\.\d+\.\d+$"))
            return new(ReleaseCheckState.InvalidRelease, Detail: "A stable semantic version is required.");
        if (!Version.TryParse(release.TagName!.TrimStart('v', 'V'), out var remote))
            return new(ReleaseCheckState.InvalidRelease);
        // Product version is three parts; avoid treating differing MSIX build revisions as a new product release.
        var local = new Version(currentVersion.Major, Math.Max(0, currentVersion.Minor),
            Math.Max(0, currentVersion.Build));
        if (remote <= local)
            return new(ReleaseCheckState.UpToDate);

        var filename = $"FlowAndroid-{remote.Major}.{remote.Minor}.{remote.Build}-x64.msix";
        var packages = release.Assets?.Where(a => a.Name == filename && a.Size > 0 &&
            IsExpectedAsset(a.Url, filename)).ToArray() ?? [];
        var hashes = release.Assets?.Where(a => a.Name == "SHA256SUMS.txt" &&
            IsExpectedAsset(a.Url, "SHA256SUMS.txt")).ToArray() ?? [];
        if (packages.Length != 1 || hashes.Length != 1 || packages[0].Size > MaxMsixBytes)
            return new(ReleaseCheckState.UnsupportedPackage,
                Detail: "The signed MSIX or SHA256SUMS is missing or ambiguous.");
        return new(ReleaseCheckState.UpdateAvailable, new UpdateRelease(remote,
            release.TagName, release.Body ?? "", new(filename, new Uri(packages[0].Url!), packages[0].Size),
            new("SHA256SUMS.txt", new Uri(hashes[0].Url!), hashes[0].Size)));
    }

    private static bool IsExpectedAsset(string? url, string expected)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps || uri.Host != "github.com" ||
            uri.Query.Length > 0 || uri.Fragment.Length > 0) return false;
        return uri.AbsolutePath.StartsWith("/KiYouJyo/FlowAndroid/releases/download/", StringComparison.Ordinal) &&
            uri.AbsolutePath.EndsWith("/" + expected, StringComparison.Ordinal);
    }

    public async Task<string> DownloadVerifiedAsync(
        UpdateRelease release, string destinationDirectory, IProgress<UpdateDownloadProgress>? progress = null,
        CancellationToken ct = default)
    {
        Directory.CreateDirectory(destinationDirectory);
        var expectedHash = await FetchExpectedChecksumAsync(release, ct).ConfigureAwait(false);
        var destination = Path.Combine(destinationDirectory, release.PackageAsset.Name);
        var partial = destination + "." + Guid.NewGuid().ToString("N") + ".partial";
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, release.PackageAsset.Url);
            req.Headers.UserAgent.ParseAdd("FlowAndroid-Updater/1.0");
            using var resp = await _client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();
            if (resp.Content.Headers.ContentLength is > MaxMsixBytes)
                throw new InvalidDataException("MSIX package is too large.");
            await using var input = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            await using (var output = new FileStream(partial, FileMode.CreateNew, FileAccess.Write, FileShare.None, 131072, true))
            using (var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
            {
                var buffer = new byte[131072];
                long total = 0;
                int n;
                while ((n = await input.ReadAsync(buffer, ct).ConfigureAwait(false)) > 0)
                {
                    total += n;
                    if (total > MaxMsixBytes)
                        throw new InvalidDataException("MSIX download exceeded safety limit.");
                    await output.WriteAsync(buffer.AsMemory(0, n), ct).ConfigureAwait(false);
                    sha.AppendData(buffer, 0, n);
                    progress?.Report(new(total, resp.Content.Headers.ContentLength));
                }
                await output.FlushAsync(ct).ConfigureAwait(false);
                if (total != release.PackageAsset.Size ||
                    (resp.Content.Headers.ContentLength is long h && h != total))
                    throw new EndOfStreamException("Release MSIX is incomplete.");
                var digest = sha.GetHashAndReset();
                if (!CryptographicOperations.FixedTimeEquals(digest, Convert.FromHexString(expectedHash)))
                    throw new InvalidDataException("Release MSIX SHA-256 does not match SHA256SUMS.");
            }
            if (File.Exists(destination)) File.Delete(destination);
            File.Move(partial, destination);
            return destination;
        }
        finally { if (File.Exists(partial)) File.Delete(partial); }
    }

    public async Task<string> FetchExpectedChecksumAsync(UpdateRelease release, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, release.ChecksumAsset.Url);
        req.Headers.UserAgent.ParseAdd("FlowAndroid-Updater/1.0");
        using var resp = await _client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
        if (resp.Content.Headers.ContentLength is > 65536)
            throw new InvalidDataException("Unexpectedly large checksum file.");
        var manifest = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        if (manifest.Length > 65536) throw new InvalidDataException("Checksum manifest exceeded limit.");
        return ParseChecksum(manifest, release.PackageAsset.Name)
            ?? throw new InvalidDataException("Expected signed MSIX checksum is not unique or missing.");
    }

    public static string? ParseChecksum(string manifest, string filename)
    {
        var matches = manifest.Split(['\r','\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(line => Regex.Match(line.Trim(),
                @"^(?<digest>[0-9a-fA-F]{64})\s+\*?" + Regex.Escape(filename) + "$"))
            .Where(m => m.Success).Select(m => m.Groups["digest"].Value).ToArray();
        return matches.Length == 1 ? matches[0].ToUpperInvariant() : null;
    }
}

public sealed record GitHubReleasePayload(
    [property: JsonPropertyName("tag_name")] string? TagName,
    [property: JsonPropertyName("prerelease")] bool Prerelease,
    [property: JsonPropertyName("draft")] bool Draft,
    [property: JsonPropertyName("body")] string? Body,
    [property: JsonPropertyName("assets")] GitHubAssetPayload[]? Assets);
public sealed record GitHubAssetPayload(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("browser_download_url")] string? Url,
    [property: JsonPropertyName("size")] long Size);
