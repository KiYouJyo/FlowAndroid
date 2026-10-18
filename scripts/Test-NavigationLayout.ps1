$ErrorActionPreference = 'Stop'
$xaml = Get-Content 'src/FlowAndroid.App/MainWindow.xaml' -Raw -Encoding UTF8
$code = Get-Content 'src/FlowAndroid.App/MainWindow.xaml.cs' -Raw -Encoding UTF8
$strings = Get-Content 'src/FlowAndroid.App/Localization/AppStrings.cs' -Raw -Encoding UTF8
if ($xaml -notmatch 'IsSettingsVisible="True"' -or $xaml -notmatch '<NavigationView.FooterMenuItems>') {
    throw 'Expected native Settings and bottom FooterMenuItems.'
}
if ($xaml -match '<NavigationView.PaneFooter>' -or $xaml -match '<NavigationViewItem[^>]*Tag="about"[^>]*>[\s\S]*?</NavigationView.MenuItems>') {
    throw 'About must be in the footer, with no language picker in the navigation pane.'
}
$footer = [regex]::Match($xaml, '<NavigationView.FooterMenuItems>(?<inside>[\s\S]*?)</NavigationView.FooterMenuItems>')
if (!$footer.Success -or $footer.Groups['inside'].Value -notmatch 'x:Name="AboutNav"') {
    throw 'About is not pinned in the footer.'
}
$settings = [regex]::Match($xaml, '<ScrollViewer x:Name="SettingsPanel"[\s\S]*?</ScrollViewer>')
if (!$settings.Success -or $settings.Value -notmatch 'x:Name="LanguageSelector"' -or
    $settings.Value -notmatch 'x:Name="ThemeSelector"') {
    throw 'Language and theme must appear on the standalone Settings page.'
}
if ($code -notmatch 'args\.IsSettingsSelected' -or $code -notmatch 'SettingsPanel\.Visibility' -or
    $code -notmatch 'PreferencesStore\.Save' -or $code -notmatch 'PreferencesStore\.Load') {
    throw 'Settings navigation or persistence wiring missing.'
}
foreach ($key in @('settings','settingsDescription','appearance','languageHint','themeSystem','themeLight','themeDark')) {
    if (([regex]::Matches($strings, '\["' + $key + '"\]')).Count -ne 3) {
        throw "Incomplete three-language Settings localization: $key"
    }
}
Write-Host 'Native About/Settings footer, separate Settings page and persistence wiring: OK.'
