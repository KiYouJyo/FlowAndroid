using System.Text.RegularExpressions;

namespace FlowAndroid.Core.Devices;

public static partial class DeviceOutputParser
{
    public static IReadOnlyList<AndroidDevice> ParseAdbDevices(string output)
    {
        var found = new List<AndroidDevice>();
        foreach (var line in output.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.StartsWith("List of devices", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("* daemon", StringComparison.OrdinalIgnoreCase))
                continue;
            var parts = Whitespace().Split(line.Trim());
            if (parts.Length < 2 || parts[0].StartsWith('*') || parts[0].StartsWith("error:", StringComparison.OrdinalIgnoreCase))
                continue;
            if (parts[1] is not ("device" or "offline" or "unauthorized" or "recovery" or "sideload" or "bootloader"))
                continue;
            found.Add(new AndroidDevice(parts[0], "adb", parts[1]));
        }
        return found;
    }

    public static IReadOnlyList<AndroidDevice> ParseFastbootDevices(string output)
    {
        var found = new List<AndroidDevice>();
        foreach (var line in output.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = Whitespace().Split(line.Trim());
            if (parts.Length < 2 || parts[1] != "fastboot")
                continue;
            found.Add(new AndroidDevice(parts[0], "fastboot", "fastboot"));
        }
        return found;
    }

    public static string ParseFastbootProduct(string output)
    {
        foreach (var line in output.Split('\n'))
        {
            var match = FastbootProduct().Match(line);
            if (match.Success)
                return match.Groups["product"].Value;
        }
        return "";
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    [GeneratedRegex(@"(?:\(bootloader\)\s*)?product:\s*(?<product>[a-zA-Z0-9_-]+)", RegexOptions.IgnoreCase)]
    private static partial Regex FastbootProduct();
}
