namespace FlowAndroid.Core.Devices;

/// <summary>
/// Informational Pixel device table, not authorization for factory flashing.
/// Unknown or future products require their own verified model/variant policy.
/// </summary>
public static class PixelDeviceCatalog
{
    private static readonly IReadOnlyDictionary<string, string> Names =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["oriole"] = "Pixel 6", ["raven"] = "Pixel 6 Pro", ["bluejay"] = "Pixel 6a",
            ["panther"] = "Pixel 7", ["cheetah"] = "Pixel 7 Pro", ["lynx"] = "Pixel 7a",
            ["felix"] = "Pixel Fold", ["tangorpro"] = "Pixel Tablet",
            ["shiba"] = "Pixel 8", ["husky"] = "Pixel 8 Pro", ["akita"] = "Pixel 8a",
            ["tokay"] = "Pixel 9", ["caiman"] = "Pixel 9 Pro", ["komodo"] = "Pixel 9 Pro XL",
            ["comet"] = "Pixel 9 Pro Fold", ["tegu"] = "Pixel 9a",
            ["frankel"] = "Pixel 10", ["blazer"] = "Pixel 10 Pro",
            ["mustang"] = "Pixel 10 Pro XL", ["rango"] = "Pixel 10 Pro Fold",
            ["stallion"] = "Pixel 10a"
        };

    public static string? GetModel(string codename) =>
        Names.TryGetValue(codename, out var name) ? name : null;

    public enum RollbackNotice { Unknown, Pixel6Or8May2025, Pixel10May2026 }

    public static RollbackNotice GetRollbackNotice(string codename) =>
        codename.ToLowerInvariant() switch
        {
            "oriole" or "raven" or "bluejay" or "shiba" or "husky" or "akita"
                => RollbackNotice.Pixel6Or8May2025,
            "frankel" or "blazer" or "mustang" or "rango"
                => RollbackNotice.Pixel10May2026,
            _ => RollbackNotice.Unknown
        };
}
