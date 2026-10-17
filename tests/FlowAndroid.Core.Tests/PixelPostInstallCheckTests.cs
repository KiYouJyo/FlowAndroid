using FlowAndroid.Core.Devices;
using Xunit;

namespace FlowAndroid.Core.Tests;

public class PixelPostInstallCheckTests
{
    private static DeviceSnapshot Snapshot(string serial, string fingerprint) => new(
        new(true, "adb"), new(true, "fastboot"),
        [new AndroidDevice(serial,"adb","device","shiba","Pixel 8","Google","16",1800000000,90,fingerprint)]);

    [Fact]
    public void MatchChecksOriginalPhoneAndExactBuild() =>
        Assert.Equal(PostInstallStatus.Verified, PixelPostInstallCheck.Evaluate(
            Snapshot("one","google/shiba/shiba:16/ABC/123:user/release-keys"), "one", "google/shiba/shiba:16/ABC/123:user/release-keys").Status);

    [Fact]
    public void DifferentPhoneFailsClosed() =>
        Assert.Equal(PostInstallStatus.WrongDevice, PixelPostInstallCheck.Evaluate(
            Snapshot("different","good"),"one","good").Status);

    [Fact]
    public void WrongBuildNeverReportsSuccess() =>
        Assert.Equal(PostInstallStatus.FingerprintMismatch, PixelPostInstallCheck.Evaluate(
            Snapshot("one","older"),"one","newer").Status);
}
