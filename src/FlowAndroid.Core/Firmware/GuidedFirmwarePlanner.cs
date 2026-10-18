using System.Text.RegularExpressions;
using FlowAndroid.Core.Devices;

namespace FlowAndroid.Core.Firmware;

public enum GuideChoiceStatus { ConnectDevice, UnsupportedDevice, NoCatalog, NoStableBuild, ReviewVariants, ReadyToDownload }

public sealed record FirmwareChoice(GuideChoiceStatus Status, string Explanation,
    IReadOnlyList<OfficialFirmware> Candidates, OfficialFirmware? Suggested = null);

/// <summary>
/// Never guess a SKU/carrier variant. The suggested file is an informational candidate,
/// not an OTA transfer permit. Device properties + downloaded metadata are rechecked later.
/// </summary>
public static partial class GuidedFirmwarePlanner
{
    public static FirmwareChoice Choose(DeviceSnapshot? devices, IReadOnlyList<OfficialFirmware>? catalog)
    {
        if (devices is null || !devices.HasOneOnlinePixel || string.IsNullOrWhiteSpace(devices.Devices[0].Codename))
            return new(GuideChoiceStatus.ConnectDevice,
                "Connect exactly one authorized Google Pixel in normal Android mode.", []);
        var device = devices.Devices[0];
        if (PixelDeviceCatalog.GetModel(device.Codename) is null)
            return new(GuideChoiceStatus.UnsupportedDevice, "Unknown Pixel variant. Do not guess an image.", []);
        if (catalog is null || catalog.Count == 0)
            return new(GuideChoiceStatus.NoCatalog, "Read the official Google OTA catalog.", []);

        var candidates = catalog.Where(x => x.Type == GoogleImageType.Ota &&
            x.Codename.Equals(device.Codename, StringComparison.OrdinalIgnoreCase) &&
            !IsExperimental(x.FileName) &&
            BuildDate().IsMatch(x.FileName) &&
            IsGoogleDownload(x.DownloadUrl)).ToList();
        if (candidates.Count == 0)
            return new(GuideChoiceStatus.NoStableBuild,
                "No recognized production full OTA image exists for this device.", []);

        var sorted = candidates.OrderByDescending(x => BuildDate().Match(x.FileName).Groups["date"].Value, StringComparer.Ordinal)
            .ThenBy(x => x.FileName, StringComparer.OrdinalIgnoreCase).ToList();
        var latest = BuildDate().Match(sorted[0].FileName).Groups["date"].Value;
        var top = sorted.Where(x => BuildDate().Match(x.FileName).Groups["date"].Value == latest).ToArray();
        // Multiple same-date releases commonly indicate regional/carrier or security variants.
        // The generic app cannot infer a safe choice merely from marketing model/codename.
        if (top.Length != 1)
            return new(GuideChoiceStatus.ReviewVariants,
                "Multiple builds share the newest release date. Carrier/regional compatibility is unknown; no automatic selection.", sorted);

        // Even one 'latest' candidate is not automatically safe if the phone runs a different
        // build channel. A user must still inspect and approve the release in the UI.
        return new(GuideChoiceStatus.ReadyToDownload,
            "One newest production candidate identified. Review its build and verify its contents before transfer.",
            sorted, top[0]);
    }

    public static bool IsExperimental(string filename) =>
        filename.Contains("_beta", StringComparison.OrdinalIgnoreCase) ||
        filename.Contains("-beta", StringComparison.OrdinalIgnoreCase) ||
        filename.Contains("preview", StringComparison.OrdinalIgnoreCase) ||
        filename.Contains("canary", StringComparison.OrdinalIgnoreCase) ||
        filename.Contains("developerpreview", StringComparison.OrdinalIgnoreCase);

    public static bool IsGoogleDownload(Uri url) =>
        url.Scheme == Uri.UriSchemeHttps &&
        url.Host is ("dl.google.com" or "googledownloads.cn") &&
        url.AbsolutePath.StartsWith("/dl/android/aosp/", StringComparison.Ordinal) &&
        string.IsNullOrEmpty(url.Query) && string.IsNullOrEmpty(url.Fragment);

    [GeneratedRegex(@"(?i)[.-](?<date>2[4-9][0-1][0-9][0-3][0-9])(?:[.-]|$)")]
    private static partial Regex BuildDate();
}
