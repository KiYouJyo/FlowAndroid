using System.IO.Compression;
using System.Security.Cryptography;

namespace FlowAndroid.Core.Firmware;

/// <summary>Inspects a Pixel full OTA ZIP without extraction or execution.</summary>
public sealed class OtaPackageInspector
{
    private const string MetadataPath = "META-INF/com/android/metadata";
    private const long MaxMetadataSize = 128 * 1024;

    public async Task<OtaPackage> InspectAsync(string path, CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            throw new FileNotFoundException("The selected firmware file does not exist.", path);
        if (!System.IO.Path.GetExtension(path).Equals(".zip", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("The package must be a .zip file.");

        string metadata;
        await using (var stream = File.OpenRead(path))
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false))
        {
            var entry = archive.GetEntry(MetadataPath)
                ?? throw new InvalidDataException("Missing Android OTA metadata; not a recognized full OTA archive.");
            if (entry.Length <= 0 || entry.Length > MaxMetadataSize)
                throw new InvalidDataException("Unexpected OTA metadata length.");
            using var reader = new StreamReader(entry.Open());
            metadata = await reader.ReadToEndAsync(token).ConfigureAwait(false);
            if (metadata.Length > MaxMetadataSize)
                throw new InvalidDataException("OTA metadata exceeded the inspection limit.");
        }

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in metadata.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var equals = line.IndexOf('=');
            if (equals > 0)
                values[line[..equals].Trim()] = line[(equals + 1)..].Trim();
        }

        if (!values.TryGetValue("pre-device", out var codename) || string.IsNullOrWhiteSpace(codename))
            throw new InvalidDataException("OTA metadata does not declare pre-device.");
        var otaType = values.GetValueOrDefault("ota-type", "unknown");
        var build = values.GetValueOrDefault("post-build", "(not provided)");

        await using var file = File.OpenRead(path);
        var hash = await SHA256.HashDataAsync(file, token).ConfigureAwait(false);
        return new OtaPackage(
            System.IO.Path.GetFullPath(path),
            codename,
            otaType,
            build,
            Convert.ToHexStringLower(hash),
            file.Length);
    }
}
