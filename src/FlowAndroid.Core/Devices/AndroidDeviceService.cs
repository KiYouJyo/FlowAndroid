using FlowAndroid.Core.Process;

namespace FlowAndroid.Core.Devices;

/// <summary>Explicit, read-only command allowlist; never accepts arbitrary commands from UI.</summary>
public sealed class AndroidDeviceService
{
    private readonly ICommandRunner _runner;
    private readonly string _adb;
    public string AdbExecutable => _adb;
    private readonly string _fastboot;

    public AndroidDeviceService(ICommandRunner runner, string? toolDirectory = null)
    {
        _runner = runner;
        _adb = ResolveTool("adb", toolDirectory);
        _fastboot = ResolveTool("fastboot", toolDirectory);
    }

    private static string ResolveTool(string name, string? directory)
    {
        var binary = OperatingSystem.IsWindows() ? name + ".exe" : name;
        if (!string.IsNullOrWhiteSpace(directory))
            return Path.Combine(directory, binary);
        var local = Path.Combine(AppContext.BaseDirectory, "platform-tools", binary);
        return File.Exists(local) ? local : name;
    }

    public async Task<DeviceSnapshot> ScanAsync(CancellationToken token = default)
    {
        var adb = await CheckToolAsync(_adb, ["version"], token).ConfigureAwait(false);
        var fastboot = await CheckToolAsync(_fastboot, ["--version"], token).ConfigureAwait(false);

        var devices = new List<AndroidDevice>();
        if (adb.Available)
        {
            var list = await TryRunAsync(_adb, ["devices", "-l"], token).ConfigureAwait(false);
            if (list is { Succeeded: true })
            {
                var parsed = DeviceOutputParser.ParseAdbDevices(list.StandardOutput);
                foreach (var device in parsed)
                {
                    if (!device.IsOnlineAdb)
                    {
                        devices.Add(device);
                        continue;
                    }
                    // Only query properties from ADB devices that are already authorized.
                    var codename = await GetPropAsync(device.Serial, "ro.product.device", token).ConfigureAwait(false);
                    var model = await GetPropAsync(device.Serial, "ro.product.model", token).ConfigureAwait(false);
                    var manufacturer = await GetPropAsync(device.Serial, "ro.product.manufacturer", token).ConfigureAwait(false);
                    var version = await GetPropAsync(device.Serial, "ro.build.version.release", token).ConfigureAwait(false);
                    var rawBuildDate = await GetPropAsync(device.Serial, "ro.build.date.utc", token).ConfigureAwait(false);
                    long.TryParse(rawBuildDate, out var buildTimestamp);
                    devices.Add(device with
                    {
                        Codename = codename,
                        Model = model,
                        Manufacturer = manufacturer,
                        AndroidVersion = version,
                        BuildTimestamp = buildTimestamp
                    });
                }
            }
            else
                adb = new ToolHealth(false, list?.CombinedOutput.Trim() ?? "ADB device enumeration failed.");
        }

        if (fastboot.Available)
        {
            var list = await TryRunAsync(_fastboot, ["devices", "-l"], token).ConfigureAwait(false);
            if (list is { Succeeded: true })
            {
                foreach (var device in DeviceOutputParser.ParseFastbootDevices(list.StandardOutput))
                {
                    var product = await TryRunAsync(_fastboot, ["-s", device.Serial, "getvar", "product"], token)
                        .ConfigureAwait(false);
                    var productCode = DeviceOutputParser.ParseFastbootProduct(product?.CombinedOutput ?? "");
                    var knownName = PixelDeviceCatalog.GetModel(productCode);
                    devices.Add(device with
                    {
                        Codename = productCode,
                        Model = knownName ?? "",
                        Manufacturer = knownName is null ? "" : "Google"
                    });
                }
            }
            else
                fastboot = new ToolHealth(false, list?.CombinedOutput.Trim() ?? "Fastboot device enumeration failed.");
        }
        return new DeviceSnapshot(adb, fastboot, devices);
    }

    private async Task<string> GetPropAsync(string serial, string key, CancellationToken token)
    {
        var result = await TryRunAsync(_adb, ["-s", serial, "shell", "getprop", key], token)
            .ConfigureAwait(false);
        return result is { Succeeded: true } ? result.StandardOutput.Trim() : "";
    }

    private async Task<ToolHealth> CheckToolAsync(string tool, string[] arguments, CancellationToken token)
    {
        var result = await TryRunAsync(tool, arguments, token).ConfigureAwait(false);
        return result is { Succeeded: true }
            ? new ToolHealth(true, result.StandardOutput.Split('\n')[0].Trim())
            : new ToolHealth(false, result?.CombinedOutput.Trim() ?? tool + " was not found.");
    }

    private async Task<CommandResult?> TryRunAsync(string tool, string[] arguments, CancellationToken token)
    {
        try
        {
            return await _runner.RunAsync(tool, arguments, TimeSpan.FromSeconds(12), token)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ex is TimeoutException or System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return new CommandResult(-1, "", ex.Message);
        }
    }
}
