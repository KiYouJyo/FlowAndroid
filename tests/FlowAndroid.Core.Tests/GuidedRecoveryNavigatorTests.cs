using FlowAndroid.Core.Devices;
using FlowAndroid.Core.Firmware;
using FlowAndroid.Core.Process;
using Xunit;

namespace FlowAndroid.Core.Tests;

public class GuidedRecoveryNavigatorTests
{
    private sealed class FakeRunner : ICommandRunner
    {
        public string Device = "serial device";
        public List<string> Calls { get; } = [];
        public Task<CommandResult> RunAsync(string exe, IReadOnlyList<string> args, TimeSpan timeout,
            CancellationToken ct = default)
        {
            Calls.Add(string.Join(" ", args));
            return Task.FromResult(args[0] == "devices"
                ? new CommandResult(0,"List of devices attached\n"+Device+"\n","")
                : new CommandResult(0,"",""));
        }
    }
    private static DeviceSnapshot Snap(int battery = 90) => new(
        new(true,"adb"),new(true,"fastboot"),
        [new AndroidDevice("serial","adb","device","shiba","Pixel 8","Google","16",1800000000,battery)]);
    private static OtaPackage Ota => new("ota.zip","shiba","AB","build",new string('a',64),200,1800000100);

    [Fact]
    public async Task ConfirmedMatchingDeviceCanRequestRecovery()
    {
        var mock = new FakeRunner();
        var r = await new GuidedRecoveryNavigator(mock, "adb").EnterRecoveryAsync(Snap(), Ota, true,true,true);
        Assert.Equal(RecoveryNavigationStatus.Started,r.Status);
        Assert.Equal(["devices -l","-s serial reboot recovery"],mock.Calls);
    }
    [Fact]
    public async Task DifferentSerialBlocksAnyReboot()
    {
        var mock = new FakeRunner { Device = "different device" };
        var r = await new GuidedRecoveryNavigator(mock, "adb").EnterRecoveryAsync(Snap(), Ota, true,true,true);
        Assert.Equal(RecoveryNavigationStatus.Blocked,r.Status);
        Assert.Single(mock.Calls);
    }
    [Fact]
    public async Task MissingBackupOrLowBatteryNeverCallsAdb()
    {
        var mock = new FakeRunner();
        Assert.Equal(RecoveryNavigationStatus.Blocked,
            (await new GuidedRecoveryNavigator(mock,"adb").EnterRecoveryAsync(Snap(),Ota,true,false,true)).Status);
        Assert.Equal(RecoveryNavigationStatus.Blocked,
            (await new GuidedRecoveryNavigator(mock,"adb").EnterRecoveryAsync(Snap(30),Ota,true,true,true)).Status);
        Assert.Empty(mock.Calls);
    }
}
