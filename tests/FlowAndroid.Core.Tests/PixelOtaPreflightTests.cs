using FlowAndroid.Core.Devices;
using FlowAndroid.Core.Firmware;
using FlowAndroid.Core.Safety;
using Xunit;

namespace FlowAndroid.Core.Tests;

public class PixelOtaPreflightTests
{
    private static DeviceSnapshot Pixel(string code = "shiba") => new(
        new ToolHealth(true, "ADB"), new ToolHealth(true, "Fastboot"),
        [new AndroidDevice("abc", "adb", "device", code, "Pixel 8", "Google", "16")]);

    private static OtaPackage Package(string code = "shiba") =>
        new("ota.zip", code, "AB", "google/shiba/build", new string('a', 64), 1);

    [Fact]
    public void MatchingDeviceIsOnlyCompatibleForReview()
    {
        var result = PixelOtaPreflight.Evaluate(Pixel(), Package());
        Assert.Equal(PreflightStatus.CompatibleForReview, result.Status);
        Assert.Contains("not verified", result.Explanation);
    }

    [Fact]
    public void MismatchFailsClosed()
    {
        Assert.Equal(PreflightStatus.FirmwareMismatch,
            PixelOtaPreflight.Evaluate(Pixel(), Package("husky")).Status);
    }

    [Fact]
    public void MultipleDevicesFailClosed()
    {
        var snapshot = Pixel() with
        {
            Devices = [Pixel().Devices[0], new AndroidDevice("def", "adb", "unauthorized")]
        };
        Assert.Equal(PreflightStatus.MultipleDevices,
            PixelOtaPreflight.Evaluate(snapshot, Package()).Status);
    }

    [Fact]
    public void NotPixelFailsClosed()
    {
        var snapshot = Pixel() with
        {
            Devices = [new AndroidDevice("abc", "adb", "device", "shiba", "Phone", "Other", "16")]
        };
        Assert.Equal(PreflightStatus.NotPixel,
            PixelOtaPreflight.Evaluate(snapshot, Package()).Status);
    }

    [Fact]
    public void MissingPackageFailsClosed()
    {
        Assert.Equal(PreflightStatus.MissingPackage,
            PixelOtaPreflight.Evaluate(Pixel(), null).Status);
    }

    [Fact]
    public void FastbootIsNotAnOtaInstallTarget()
    {
        var snapshot = Pixel() with
        {
            Devices = [new AndroidDevice("abc", "fastboot", "fastboot", "shiba")]
        };
        Assert.Equal(PreflightStatus.UnsupportedTransport,
            PixelOtaPreflight.Evaluate(snapshot, Package()).Status);
    }
}
