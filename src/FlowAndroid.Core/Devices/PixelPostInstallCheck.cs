namespace FlowAndroid.Core.Devices;

public enum PostInstallStatus { Verified, WaitingForAndroid, WrongDevice, FingerprintMismatch, InvalidExpectedBuild }

public sealed record PostInstallResult(PostInstallStatus Status, string Description)
{
    public bool Success => Status == PostInstallStatus.Verified;
}

public static class PixelPostInstallCheck
{
    public static PostInstallResult Evaluate(DeviceSnapshot snapshot, string expectedSerial, string expectedBuildFingerprint)
    {
        if (string.IsNullOrWhiteSpace(expectedSerial) || string.IsNullOrWhiteSpace(expectedBuildFingerprint) ||
            expectedBuildFingerprint == "(not provided)")
            return new(PostInstallStatus.InvalidExpectedBuild, "Target OTA build fingerprint was not available.");
        if (snapshot.Devices.Count != 1 || !snapshot.Devices[0].IsOnlineAdb)
            return new(PostInstallStatus.WaitingForAndroid, "One authorized Pixel must boot normally before verification.");
        var device = snapshot.Devices[0];
        if (!device.IsGooglePixel || !string.Equals(device.Serial, expectedSerial, StringComparison.Ordinal))
            return new(PostInstallStatus.WrongDevice, "The connected phone is not the originally updated Pixel.");
        if (!string.Equals(device.BuildFingerprint, expectedBuildFingerprint, StringComparison.Ordinal))
            return new(PostInstallStatus.FingerprintMismatch, "The running Android build is different from the OTA target.");
        return new(PostInstallStatus.Verified, "The authorized Pixel booted with the exact expected Android build fingerprint.");
    }
}
