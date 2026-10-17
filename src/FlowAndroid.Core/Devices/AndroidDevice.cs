namespace FlowAndroid.Core.Devices;

public sealed record AndroidDevice(
    string Serial,
    string Transport,
    string State,
    string Codename = "",
    string Model = "",
    string Manufacturer = "",
    string AndroidVersion = "",
    long BuildTimestamp = 0,
    int BatteryPercent = -1)
{
    public bool IsOnlineAdb => Transport == "adb" && State == "device";
    public bool IsGooglePixel =>
        Manufacturer.Equals("Google", StringComparison.OrdinalIgnoreCase) &&
        Model.StartsWith("Pixel", StringComparison.OrdinalIgnoreCase);
}

public sealed record ToolHealth(bool Available, string Description);

public sealed record DeviceSnapshot(
    ToolHealth Adb,
    ToolHealth Fastboot,
    IReadOnlyList<AndroidDevice> Devices)
{
    public bool HasOneOnlinePixel =>
        Devices.Count == 1 && Devices[0].IsOnlineAdb && Devices[0].IsGooglePixel;
}
