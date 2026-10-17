using FlowAndroid.Core.Firmware;

try
{
    var ota = await GoogleFirmwareCatalog.FetchAsync(GoogleImageType.Ota);
    var factory = await GoogleFirmwareCatalog.FetchAsync(GoogleImageType.Factory);
    Console.WriteLine($"Official Google OTA entries: {ota.Count}, factory entries: {factory.Count}");
    if (ota.Count < 50 || factory.Count < 50)
        throw new InvalidDataException("Unexpectedly small official firmware catalog; fail closed.");
    var known = ota.SingleOrDefault(x =>
        x.FileName == "cubs-ota-cd1a.261005.003-3324f260.zip" &&
        x.Sha256 == "3324f2601ea2ec76303a3e88e0deffb608d6c15568850f9352aaea65def1e67b");
    if (known is null)
        throw new InvalidDataException("Known public Pixel 11 OTA checksum absent or changed.");
    Console.WriteLine("Live official Google catalog and known checksum: OK.");
}
catch (Exception ex)
{
    Console.Error.WriteLine("Official catalog smoke test FAILED (no firmware operations allowed): " + ex.Message);
    Environment.ExitCode = 1;
}
