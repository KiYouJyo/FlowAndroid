using FlowAndroid.Core.Process;
using FlowAndroid.Core.Safety;
using FlowAndroid.Core.Firmware;

namespace FlowAndroid.Core.Devices;

public enum RecoveryNavigationStatus { Started, Blocked, Error }
public sealed record RecoveryNavigationResult(RecoveryNavigationStatus Status, string Message);

/// <summary>One narrowly scoped, explicitly confirmed transport command; no partition writes.</summary>
public sealed class GuidedRecoveryNavigator
{
    private readonly ICommandRunner _runner;
    private readonly string _adb;
    public GuidedRecoveryNavigator(ICommandRunner runner, string adb) { _runner = runner; _adb = adb; }

    public async Task<RecoveryNavigationResult> EnterRecoveryAsync(
        DeviceSnapshot? snapshot, OtaPackage? ota, bool officialChecksumVerified,
        bool backupConfirmed, bool termsAccepted,
        CancellationToken cancellationToken = default)
    {
        if (!officialChecksumVerified || !backupConfirmed || !termsAccepted)
            return new(RecoveryNavigationStatus.Blocked, "Official hash, backup and license confirmations are required.");
        if (!PixelOtaPreflight.Evaluate(snapshot, ota).IsCompatibleForReview)
            return new(RecoveryNavigationStatus.Blocked, "Device or OTA is not ready.");
        var d = snapshot!.Devices[0];
        if (d.BatteryPercent < 50 || d.BatteryPercent > 100)
            return new(RecoveryNavigationStatus.Blocked, "Battery must be readable and at least 50%.");
        if (d.BuildTimestamp <= 0 || ota!.PostTimestamp <= 0 || ota.PostTimestamp < d.BuildTimestamp)
            return new(RecoveryNavigationStatus.Blocked,
                "Build date is missing or firmware appears older than the current Android system.");
        // Recheck the exact ADB device binding immediately before rebooting.
        try
        {
            var list = await _runner.RunAsync(_adb, ["devices", "-l"],
                TimeSpan.FromSeconds(15), cancellationToken).ConfigureAwait(false);
            var devices = DeviceOutputParser.ParseAdbDevices(list.StandardOutput);
            if (!list.Succeeded || devices.Count != 1 || devices[0].State != "device" ||
                !string.Equals(devices[0].Serial, d.Serial, StringComparison.Ordinal))
                return new(RecoveryNavigationStatus.Blocked, "The connected device changed; rescan before proceeding.");
            var result = await _runner.RunAsync(_adb, ["-s", d.Serial, "reboot", "recovery"],
                TimeSpan.FromSeconds(20), cancellationToken).ConfigureAwait(false);
            return result.Succeeded
                ? new(RecoveryNavigationStatus.Started,
                    "Recovery requested. On your phone, open the Recovery menu and select Apply update from ADB.")
                : new(RecoveryNavigationStatus.Error,
                    "Could not request Recovery: " + result.StandardError.Trim());
        }
        catch (Exception ex) when (ex is TimeoutException or IOException or System.ComponentModel.Win32Exception)
        {
            return new(RecoveryNavigationStatus.Error, "ADB could not switch to Recovery: " + ex.Message);
        }
    }
}
