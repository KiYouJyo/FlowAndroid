using FlowAndroid.Core.Devices;
using FlowAndroid.Core.Firmware;

namespace FlowAndroid.Core.Safety;

public enum PreflightStatus
{
    CompatibleForReview,
    ToolsUnavailable,
    NoDevice,
    MultipleDevices,
    UnsupportedTransport,
    NotAuthorized,
    NotPixel,
    IdentityUnknown,
    MissingPackage,
    FirmwareMismatch
}

public sealed record PreflightResult(PreflightStatus Status, string Explanation)
{
    public bool IsCompatibleForReview => Status == PreflightStatus.CompatibleForReview;
}

public static class PixelOtaPreflight
{
    /// <summary>A match is NOT authorization to sideload: firmware origin/signature is not verified.</summary>
    public static PreflightResult Evaluate(DeviceSnapshot? snapshot, OtaPackage? package)
    {
        if (snapshot is null || !snapshot.Adb.Available)
            return new(PreflightStatus.ToolsUnavailable, "ADB is unavailable.");
        if (snapshot.Devices.Count == 0)
            return new(PreflightStatus.NoDevice, "Connect a Pixel with USB debugging enabled.");
        if (snapshot.Devices.Count != 1)
            return new(PreflightStatus.MultipleDevices, "Disconnect additional Android devices.");
        var device = snapshot.Devices[0];
        if (device.Transport != "adb")
            return new(PreflightStatus.UnsupportedTransport, "Connect the phone in normal Android mode.");
        if (!device.IsOnlineAdb)
            return new(PreflightStatus.NotAuthorized, "Authorize USB debugging and reconnect.");
        if (!device.IsGooglePixel)
            return new(PreflightStatus.NotPixel, "Only recognized Google Pixel phones are supported.");
        if (string.IsNullOrWhiteSpace(device.Codename))
            return new(PreflightStatus.IdentityUnknown, "The device codename could not be read.");
        if (package is null)
            return new(PreflightStatus.MissingPackage, "Select a local official Pixel OTA ZIP.");
        var compatible = package.DeviceCodename
            .Split(['|', ','], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Any(x => string.Equals(x, device.Codename, StringComparison.OrdinalIgnoreCase));
        if (!compatible)
            return new(PreflightStatus.FirmwareMismatch, "The firmware codename does not match the connected Pixel.");
        return new(PreflightStatus.CompatibleForReview,
            "Device codename matches the OTA metadata. Provenance and signature are not verified; flashing is disabled.");
    }
}
