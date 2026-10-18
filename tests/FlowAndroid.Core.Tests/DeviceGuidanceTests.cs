using FlowAndroid.Core.Devices;
using Xunit;

namespace FlowAndroid.Core.Tests;

public sealed class DeviceGuidanceTests
{
    private static DeviceSnapshot Snap(AndroidDevice device) =>
        new(new(true,"adb"),new(true,"fastboot"),[device]);

    [Theory]
    [InlineData("adb","unauthorized",ConnectionProblem.UsbAuthorization)]
    [InlineData("adb","offline",ConnectionProblem.Offline)]
    [InlineData("adb","recovery",ConnectionProblem.RecoveryMode)]
    [InlineData("fastboot","fastboot",ConnectionProblem.FastbootMode)]
    public void DeviceStateHasSpecificBeginnerRecoveryTip(
        string transport, string state, ConnectionProblem expected)
    {
        var device=new AndroidDevice("serial",transport,state);
        Assert.Equal(expected,DeviceGuidanceEngine.Diagnose(Snap(device)).Problem);
    }

    [Fact]
    public void NoBatteryOrMultipleDevicesDoNotPermitFirmwareSearch()
    {
        var phone=new AndroidDevice("one","adb","device","shiba","Pixel 8","Google","16",1000,-1);
        Assert.Equal(ConnectionProblem.LowBattery,DeviceGuidanceEngine.Diagnose(Snap(phone)).Problem);
        var many = new DeviceSnapshot(new(true,"adb"),new(true,"fastboot"),[phone,phone with { Serial="two" }]);
        Assert.Equal(ConnectionProblem.MultiplePhones,DeviceGuidanceEngine.Diagnose(many).Problem);
        Assert.True(DeviceGuidanceEngine.Diagnose(Snap(phone with { BatteryPercent=80 })).CanSearchFirmware);
    }
}
