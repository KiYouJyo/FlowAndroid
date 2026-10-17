using System.IO.Compression;
using System.Text.RegularExpressions;

namespace FlowAndroid.Core.Firmware;

public sealed record FactoryImage(string FileName, string Codename, string Sha256, IReadOnlyList<string> Entries);

/// <summary>Never extracts or executes vendor-supplied flash-all scripts.</summary>
public static class FactoryImageInspector
{
    public static async Task<FactoryImage> InspectAsync(string path, CancellationToken ct = default)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("Factory archive not found.", path);
        var name = Path.GetFileName(path);
        var match = Regex.Match(name, @"^(?<device>[a-z0-9_]+)-.+-factory-[0-9a-f]{8}\.zip$",
            RegexOptions.IgnoreCase);
        if (!match.Success) throw new InvalidDataException("Unrecognized factory ZIP filename.");
        var names = new List<string>();
        await using (var stream = File.OpenRead(path))
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Read))
        {
            if (zip.Entries.Count > 500) throw new InvalidDataException("Too many archive members.");
            foreach (var entry in zip.Entries)
            {
                if (entry.FullName.Contains("..") || entry.FullName.StartsWith('/') || entry.FullName.Contains('\\'))
                    throw new InvalidDataException("Unsafe path in factory archive.");
                names.Add(entry.FullName);
            }
        }
        if (!names.Any(x => x.EndsWith("/flash-all.bat", StringComparison.OrdinalIgnoreCase)) ||
            !names.Any(x => x.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidDataException("Incomplete factory archive.");
        await using var source = File.OpenRead(path);
        var sha = await System.Security.Cryptography.SHA256.HashDataAsync(source, ct);
        return new(name, match.Groups["device"].Value, Convert.ToHexStringLower(sha), names);
    }
}
