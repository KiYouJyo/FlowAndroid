namespace FlowAndroid.Core.Devices;

public enum ConnectionProblem
{
    NeedScan, MissingTools, NoPhone, MultiplePhones, UsbAuthorization,
    Offline, RecoveryMode, FastbootMode, NotPixel, LowBattery, Ready
}
public sealed record DeviceGuidance(ConnectionProblem Problem, bool CanSearchFirmware);

/// <summary>Small, deterministic troubleshooting classifier. No device writes.</summary>
public static class DeviceGuidanceEngine
{
    public static DeviceGuidance Diagnose(DeviceSnapshot? snapshot)
    {
        if (snapshot is null) return new(ConnectionProblem.NeedScan, false);
        if (!snapshot.Adb.Available || !snapshot.Fastboot.Available)
            return new(ConnectionProblem.MissingTools, false);
        if (snapshot.Devices.Count == 0)
            return new(ConnectionProblem.NoPhone, false);
        if (snapshot.Devices.Count > 1)
            return new(ConnectionProblem.MultiplePhones, false);
        var d = snapshot.Devices[0];
        if (d.Transport == "fastboot")
            return new(ConnectionProblem.FastbootMode, false);
        if (d.State == "unauthorized")
            return new(ConnectionProblem.UsbAuthorization, false);
        if (d.State == "offline")
            return new(ConnectionProblem.Offline, false);
        if (d.State is "recovery" or "sideload")
            return new(ConnectionProblem.RecoveryMode, false);
        if (!d.IsOnlineAdb)
            return new(ConnectionProblem.Offline, false);
        if (!d.IsGooglePixel || string.IsNullOrWhiteSpace(d.Codename))
            return new(ConnectionProblem.NotPixel, false);
        if (d.BatteryPercent < 50 || d.BatteryPercent > 100)
            return new(ConnectionProblem.LowBattery, false);
        return new(ConnectionProblem.Ready, true);
    }
}
