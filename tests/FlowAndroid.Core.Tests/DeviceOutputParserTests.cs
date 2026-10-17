using FlowAndroid.Core.Devices;
using Xunit;

namespace FlowAndroid.Core.Tests;

public class DeviceOutputParserTests
{
    [Fact]
    public void Adb_RecognizesAuthorizedAndUnauthorizedDevices()
    {
        var devices = DeviceOutputParser.ParseAdbDevices(
            "List of devices attached\nABC123 device product:foo model:Pixel_8\nDEF456 unauthorized\n");
        Assert.Equal(2, devices.Count);
        Assert.Equal("ABC123", devices[0].Serial);
        Assert.Equal("device", devices[0].State);
        Assert.Equal("unauthorized", devices[1].State);
    }

    [Fact]
    public void Fastboot_RecognizesOnlyFastbootEntries()
    {
        var devices = DeviceOutputParser.ParseFastbootDevices("XYZ fastboot usb:1-1\n");
        Assert.Single(devices);
        Assert.Equal("XYZ", devices[0].Serial);
        Assert.Equal("fastboot", devices[0].Transport);
    }

    [Fact]
    public void FastbootProduct_ReadsStderrStyleOutput()
    {
        Assert.Equal("shiba",
            DeviceOutputParser.ParseFastbootProduct("(bootloader) product: shiba\nFinished. Total time: 0.001s"));
    }
}
