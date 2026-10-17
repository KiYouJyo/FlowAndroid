using FlowAndroid.Core.Firmware;
using FlowAndroid.Core.Process;
using FlowAndroid.Core.Safety;

namespace FlowAndroid.Core.Devices;

public enum TransferStage { NotStarted, Verifying, WaitingForRecovery, Sending, TransferDone, Blocked }

public sealed record TransferResult(TransferStage Stage, string Detail);

/// <summary>
/// Only performs an official, checksum-verified OTA ADB sideload.
/// It never unlocks, reboots, wipes, runs a script, or directly flashes a partition.
/// On transfer completion, the phone's Recovery UI is authoritative for installation status.
/// </summary>
public sealed class PixelOtaTransfer
{
    private readonly ICommandRunner _runner;
    private readonly string _adb;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public PixelOtaTransfer(ICommandRunner runner, string adb = "adb")
    {
        _runner = runner;
        _adb = adb;
    }

    public async Task<TransferResult> RunAsync(
        DeviceSnapshot original, OtaPackage ota,
        IReadOnlyList<OfficialFirmware> googleCatalog,
        bool backupConfirmed, bool googleTermsAccepted, bool recoveryConfirmed,
        IProgress<TransferStage>? progress = null, CancellationToken ct = default)
    {
        if (!await _gate.WaitAsync(0, ct)) return new(TransferStage.Blocked, "Operation already active.");
        try
        {
            var preflight = PixelOtaPreflight.Evaluate(original, ota);
            if (!preflight.IsCompatibleForReview) return new(TransferStage.Blocked, preflight.Explanation);
            if (!backupConfirmed || !googleTermsAccepted || !recoveryConfirmed)
                return new(TransferStage.Blocked, "Backup, Google terms and recovery confirmations required.");
            if (!string.Equals(ota.OtaType, "AB", StringComparison.OrdinalIgnoreCase))
                return new(TransferStage.Blocked, "Only AB full OTA packages are supported.");

            progress?.Report(TransferStage.Verifying);
            var trust = await GoogleFirmwareVerifier.VerifyAsync(ota.Path, GoogleImageType.Ota, googleCatalog, ct);
            if (!trust.Verified || trust.Official is null)
                return new(TransferStage.Blocked, trust.Explanation);
            if (!string.Equals(trust.Official.Codename, original.Devices[0].Codename, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(ota.Sha256, trust.Official.Sha256, StringComparison.OrdinalIgnoreCase))
                return new(TransferStage.Blocked, "Device codename or ZIP fingerprint changed.");

            if (original.Devices[0].BuildTimestamp <= 0 || ota.PostTimestamp <= 0)
                return new(TransferStage.Blocked, "Build timestamps unavailable; downgrade safety cannot be determined.");
            if (ota.PostTimestamp < original.Devices[0].BuildTimestamp)
                return new(TransferStage.Blocked, "Older build is blocked to prevent unsupported downgrades.");
            if (original.Devices[0].BatteryPercent < 50 || original.Devices[0].BatteryPercent > 100)
                return new(TransferStage.Blocked,
                    "Battery level is unknown or below 50%. Charge the device before updating.");

            var serial = original.Devices[0].Serial;
            progress?.Report(TransferStage.WaitingForRecovery);
            var list = await _runner.RunAsync(_adb, ["devices", "-l"], TimeSpan.FromSeconds(15), ct);
            var devices = DeviceOutputParser.ParseAdbDevices(list.StandardOutput);
            if (!list.Succeeded || devices.Count != 1 || devices[0].State != "sideload" ||
                !string.Equals(devices[0].Serial, serial, StringComparison.Ordinal))
                return new(TransferStage.Blocked,
                    "Recovery must show Apply update from ADB, with the exact original device serial.");
            ct.ThrowIfCancellationRequested();
            progress?.Report(TransferStage.Sending);

            // Once ADB starts writing, don't kill the process on an impatient UI cancellation.
            var result = await _runner.RunAsync(_adb, ["-s", serial, "sideload", ota.Path],
                TimeSpan.FromMinutes(45), CancellationToken.None);
            if (!result.Succeeded)
                return new(TransferStage.Blocked,
                    "ADB transfer failed; inspect the phone's Recovery screen: " + result.StandardError.Trim());
            progress?.Report(TransferStage.TransferDone);
            return new(TransferStage.TransferDone,
                "ADB transfer ended. Confirm installation success on the phone and manually reboot.");
        }
        catch (Exception ex) when (ex is IOException or TimeoutException or System.ComponentModel.Win32Exception)
        {
            return new(TransferStage.Blocked,
                "Interrupted/failed transfer. Check the phone before any retry: " + ex.Message);
        }
        finally { _gate.Release(); }
    }
}
