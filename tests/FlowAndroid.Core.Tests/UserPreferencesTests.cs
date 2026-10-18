using FlowAndroid.Core.Settings;
using Xunit;

namespace FlowAndroid.Core.Tests;

public sealed class UserPreferencesTests
{
    [Fact]
    public void LanguageAndThemeSurviveRestart()
    {
        var dir=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N"));
        var path=Path.Combine(dir,"preferences.json");
        try
        {
            Assert.Equal(UserPreferences.Default,PreferencesStore.Load(path));
            Assert.True(PreferencesStore.Save(new("ja-JP","dark"),path));
            Assert.Equal(new UserPreferences("ja-JP","dark"),PreferencesStore.Load(path));
        }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir,true); }
    }

    [Fact]
    public void UnknownOrCorruptSettingsUseSafeDefaults()
    {
        var dir=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N"));
        var path=Path.Combine(dir,"prefs.json");
        try
        {
            Assert.True(PreferencesStore.Save(new("bad-language","red"),path));
            Assert.Equal(UserPreferences.Default,PreferencesStore.Load(path));
            File.WriteAllText(path,"{ invalid");
            Assert.Equal(UserPreferences.Default,PreferencesStore.Load(path));
        }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir,true); }
    }
}
