using System.IO.Compression;
using FlowAndroid.Core.Firmware;
using Xunit;

namespace FlowAndroid.Core.Tests;

public class OtaPackageInspectorTests
{
    private static string BuildZip(string metadata)
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".zip");
        using var archive = ZipFile.Open(path, ZipArchiveMode.Create);
        var entry = archive.CreateEntry("META-INF/com/android/metadata");
        using var writer = new StreamWriter(entry.Open());
        writer.Write(metadata);
        return path;
    }

    [Fact]
    public async Task ReadsMetadataWithoutExtractingPayload()
    {
        var path = BuildZip("pre-device=shiba\nota-type=AB\npost-build=google/shiba/123\n");
        try
        {
            var info = await new OtaPackageInspector().InspectAsync(path);
            Assert.Equal("shiba", info.DeviceCodename);
            Assert.Equal("AB", info.OtaType);
            Assert.Equal(64, info.Sha256.Length);
            Assert.Equal("google/shiba/123", info.Build);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task RejectsZipMissingDeviceMetadata()
    {
        var path = BuildZip("ota-type=AB\n");
        try
        {
            await Assert.ThrowsAsync<InvalidDataException>(
                () => new OtaPackageInspector().InspectAsync(path));
        }
        finally { File.Delete(path); }
    }
    [Fact]
    public async Task RejectsOversizedCompressedMetadata()
    {
        var path = BuildZip("pre-device=shiba\n" + new string('x', 130_000));
        try
        {
            await Assert.ThrowsAsync<InvalidDataException>(
                () => new OtaPackageInspector().InspectAsync(path));
        }
        finally { File.Delete(path); }
    }

}
