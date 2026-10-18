using System.Text.Json;

namespace FlowAndroid.Core.Settings;

public sealed record UserPreferences(string Language, string Theme)
{
    public static UserPreferences Default { get; } = new("zh-CN", "system");

    public static UserPreferences Normalize(UserPreferences? preferences) => new(
        preferences?.Language is "zh-CN" or "ja-JP" or "en-US" ? preferences.Language : "zh-CN",
        preferences?.Theme is "system" or "light" or "dark" ? preferences.Theme : "system");
}

public static class PreferencesStore
{
    public static string DefaultFilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FlowAndroid", "preferences.json");

    public static UserPreferences Load(string? filePath = null)
    {
        try
        {
            var path = filePath ?? DefaultFilePath;
            if (!File.Exists(path)) return UserPreferences.Default;
            using var file = File.OpenRead(path);
            return UserPreferences.Normalize(JsonSerializer.Deserialize<UserPreferences>(file));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return UserPreferences.Default;
        }
    }

    public static bool Save(UserPreferences preferences, string? filePath = null)
    {
        var target = filePath ?? DefaultFilePath;
        var temp = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.WriteAllText(temp, JsonSerializer.Serialize(UserPreferences.Normalize(preferences)));
            File.Move(temp, target, overwrite: true);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
        finally
        {
            try { if (File.Exists(temp)) File.Delete(temp); }
            catch (IOException) { }
        }
    }
}
