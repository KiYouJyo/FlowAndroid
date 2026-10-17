using System.Net;
using System.Text.RegularExpressions;

namespace FlowAndroid.Core.Firmware;

public enum GoogleImageType { Ota, Factory }

public sealed record OfficialFirmware(GoogleImageType Type, string FileName, string Sha256, Uri DownloadUrl)
{
    public string Codename => FileName.Split('-')[0];
}

/// <summary>Fail-closed parser for Google Developers' official published checksums.</summary>
public static class GoogleFirmwareCatalog
{
    public const string OtaUrl = "https://developers.google.com/android/ota";
    public const string FactoryUrl = "https://developers.google.com/android/images";
    private static readonly Regex Rows = new(@"<tr\b[^>]*>(.*?)</tr>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
    private static readonly Regex Links = new(@"href\s*=\s*[""'](https://[^""']+\.zip)[""']", RegexOptions.IgnoreCase);
    private static readonly Regex Sha = new(@"(?<![0-9a-fA-F])[0-9a-fA-F]{64}(?![0-9a-fA-F])");

    public static IReadOnlyList<OfficialFirmware> Parse(string html, GoogleImageType type)
    {
        var items = new List<OfficialFirmware>();
        foreach (Match row in Rows.Matches(html))
        {
            var content = WebUtility.HtmlDecode(row.Groups[1].Value);
            var checksums = Sha.Matches(content);
            if (checksums.Count != 1) continue;
            var urls = Links.Matches(content);
            if (urls.Count != 1 || !Uri.TryCreate(urls[0].Groups[1].Value, UriKind.Absolute, out var url)) continue;
            if (url.Scheme != "https" || url.Host is not ("dl.google.com" or "googledownloads.cn") ||
                !url.AbsolutePath.StartsWith("/dl/android/aosp/", StringComparison.Ordinal) ||
                url.Query.Length != 0 || url.Fragment.Length != 0) continue;
            var name = Uri.UnescapeDataString(Path.GetFileName(url.AbsolutePath));
            if (!Regex.IsMatch(name, @"^[a-zA-Z0-9._-]+\.zip$")) continue;
            if (type == GoogleImageType.Ota && !name.Contains("-ota-", StringComparison.OrdinalIgnoreCase)) continue;
            if (type == GoogleImageType.Factory && !name.Contains("-factory-", StringComparison.OrdinalIgnoreCase)) continue;
            items.Add(new(type, name, checksums[0].Value.ToLowerInvariant(), url));
        }
        return items;
    }

    public static async Task<IReadOnlyList<OfficialFirmware>> FetchAsync(
        GoogleImageType type, CancellationToken ct = default)
    {
        // Google may present a consent-only page to non-browser clients. Both fixed hosts
        // below belong to Google; no caller-controlled URL is ever accepted.
        var primary = type == GoogleImageType.Ota ? OtaUrl : FactoryUrl;
        var secondary = type == GoogleImageType.Ota
            ? "https://developers.google.cn/android/ota?hl=en"
            : "https://developers.google.cn/android/images?hl=en";
        Exception? lastError = null;
        foreach (var page in new[] { primary, secondary })
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                using var handler = new HttpClientHandler { AllowAutoRedirect = false };
                using var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(25) };
                using var response = await http.GetAsync(page, HttpCompletionOption.ResponseHeadersRead, ct);
                response.EnsureSuccessStatusCode();
                if (response.Content.Headers.ContentLength > 20_000_000)
                    throw new InvalidDataException("Official catalog response too large.");
                await using var content = await response.Content.ReadAsStreamAsync(ct);
                using var reader = new StreamReader(content);
                var html = new System.Text.StringBuilder();
                var buffer = new char[8192];
                int n;
                while ((n = await reader.ReadAsync(buffer.AsMemory(), ct)) > 0)
                {
                    html.Append(buffer, 0, n);
                    if (html.Length > 20_000_000)
                        throw new InvalidDataException("Official catalog exceeded size limit.");
                }
                var parsed = Parse(html.ToString(), type);
                if (parsed.Count == 0)
                    throw new InvalidDataException("No trusted checksum rows found on this Google page.");
                return parsed;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidDataException)
            {
                lastError = ex;
            }
        }
        throw new InvalidDataException(
            "Unable to read a trusted Google SHA-256 catalog. Sideload remains disabled.", lastError);
    }
}
