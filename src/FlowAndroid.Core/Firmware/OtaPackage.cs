namespace FlowAndroid.Core.Firmware;

public sealed record OtaPackage(
    string Path,
    string DeviceCodename,
    string OtaType,
    string Build,
    string Sha256,
    long FileSize,
    long PostTimestamp = 0);
