using System.Text;
using FlowAndroid.Core.Devices;
using FlowAndroid.Core.Firmware;
using FlowAndroid.Core.Process;
using FlowAndroid.Core.Safety;
using FlowAndroid.Localization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;

namespace FlowAndroid;

public sealed partial class MainWindow : Window
{
    private readonly AndroidDeviceService _devices = new(new SystemCommandRunner());
    private readonly OtaPackageInspector _inspector = new();
    private DeviceSnapshot? _snapshot;
    private OtaPackage? _package;
    private IReadOnlyList<OfficialFirmware>? _trustedCatalog;
    private bool _otaVerified;
    private readonly StringBuilder _history = new();
    private string _language = "zh-CN";

    public MainWindow()
    {
        InitializeComponent();
        Title = "FlowAndroid";
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(DragRegion);
        Navigation.SelectedItem = HomeNav;
        ApplyLanguage();
        UpdateDeviceDetails();
        UpdatePackageDetails();
        UpdatePreflight();
        AddLog("FlowAndroid 0.1.0 started. Read-only diagnostic mode.");
    }

    private string T(string key) => AppStrings.Get(_language, key);

    private void LanguageSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LanguageSelector?.SelectedItem is ComboBoxItem item && item.Tag is string language)
        {
            _language = language;
            if (HomeTitle is not null)
                ApplyLanguage();
        }
    }

    private void ApplyLanguage()
    {
        HomeNav.Content = T("home");
        FirmwareNav.Content = T("firmware");
        LogsNav.Content = T("logs");
        AboutNav.Content = T("about");
        LanguageCaption.Text = T("language");
        HomeTitle.Text = T("homeTitle");
        HomeDescription.Text = T("homeDesc");
        SafetyBanner.Title = T("bannerTitle");
        SafetyBanner.Message = T("bannerBody");
        DeviceHeading.Text = T("deviceTitle");
        ModelCaption.Text = T("model");
        CodeCaption.Text = T("code");
        StateCaption.Text = T("state");
        VersionCaption.Text = T("version");
        ScanButton.Content = T("scan");
        NextStepTitle.Text = T("nextTitle");
        NextStepBody.Text = T("nextBody");
        OpenFirmwareButton.Content = T("selectOta");
        FirmwareTitle.Text = T("fwTitle");
        FirmwareDescription.Text = T("fwDesc");
        PackageTitle.Text = T("packageTitle");
        PickPackageButton.Content = T("browse");
        PreflightTitle.Text = T("preflight");
        PreflightButton.Content = T("check");
        ProvenanceWarning.Title = T("warningTitle");
        ProvenanceWarning.Message = T("warningBody");
        LogsTitle.Text = T("logsTitle");
        LogsDescription.Text = T("logsDesc");
        ClearLogsButton.Content = T("clear");
        AboutDescription.Text = T("aboutDesc");

        OfficialTitle.Text = T("officialTitle");
        OfficialHint.Text = T("officialHint");
        VerifyOfficialButton.Content = T("verifyOfficial");
        if (!_otaVerified) OfficialResult.Text = T("officialPending");
        SideloadTitle.Text = T("sideloadTitle");
        SideloadInstructions.Text = T("sideloadInstructions");
        BackupConsent.Content = T("backupConsent");
        TermsConsent.Content = T("termsConsent");
        RecoveryConsent.Content = T("recoveryConsent");
        StartSideloadButton.Content = T("sideloadStart");
        if (SideloadStatus.Text.Length == 0) SideloadStatus.Text = T("sideloadHint");
        FactoryTitle.Text = T("factoryTitle");
        FactoryWarning.Text = T("factoryWarning");
        PickFactoryButton.Content = T("factoryPick");
        if (FactoryResult.Text.Length == 0) FactoryResult.Text = T("factoryPending");

        UpdateDeviceDetails();
        UpdatePackageDetails();
        UpdatePreflight();
    }

    private void Navigation_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (HomePanel is null)
            return;
        var tag = (args.SelectedItem as NavigationViewItem)?.Tag?.ToString() ?? "home";
        HomePanel.Visibility = tag == "home" ? Visibility.Visible : Visibility.Collapsed;
        FirmwarePanel.Visibility = tag == "firmware" ? Visibility.Visible : Visibility.Collapsed;
        LogsPanel.Visibility = tag == "logs" ? Visibility.Visible : Visibility.Collapsed;
        AboutPanel.Visibility = tag == "about" ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OpenFirmwareButton_Click(object sender, RoutedEventArgs e)
    {
        Navigation.SelectedItem = FirmwareNav;
    }

    private async void ScanButton_Click(object sender, RoutedEventArgs e)
    {
        ScanButton.IsEnabled = false;
        ScanProgress.IsActive = true;
        DeviceSummary.Text = T("scanning");
        try
        {
            _snapshot = await _devices.ScanAsync();
            AddLog("Scan complete. Device count: " + _snapshot.Devices.Count);
            AddLog("ADB: " + _snapshot.Adb.Description);
            AddLog("Fastboot: " + _snapshot.Fastboot.Description);
            foreach (var d in _snapshot.Devices)
                AddLog("Device: " + d.Transport + " / " + d.State + " / " + d.Codename);
        }
        catch (Exception ex)
        {
            _snapshot = null;
            AddLog("Scan error: " + ex.Message);
            DeviceSummary.Text = T("scanError") + ": " + ex.Message;
        }
        finally
        {
            ScanButton.IsEnabled = true;
            ScanProgress.IsActive = false;
            UpdateDeviceDetails();
            UpdatePreflight();
        }
    }

    private async void PickPackageButton_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker();
        picker.FileTypeFilter.Add(".zip");
        WinRT.Interop.InitializeWithWindow.Initialize(
            picker, WinRT.Interop.WindowNative.GetWindowHandle(this));
        var file = await picker.PickSingleFileAsync();
        if (file is null)
            return;

        PickPackageButton.IsEnabled = false;
        PackageProgress.IsActive = true;
        try
        {
            // Invalidate the previous package before inspecting a new choice.
            _package = null;
            _trustedCatalog = null;
            _otaVerified = false;
            UpdateSideloadButton();
            _package = await _inspector.InspectAsync(file.Path);
            AddLog("OTA metadata inspected: " + _package.DeviceCodename);
            AddLog("SHA-256: " + _package.Sha256);
        }
        catch (Exception ex)
        {
            _package = null;
            AddLog("OTA inspection error: " + ex.Message);
            PackageName.Text = T("inspectError") + ": " + ex.Message;
        }
        finally
        {
            PickPackageButton.IsEnabled = true;
            PackageProgress.IsActive = false;
            if (_package is not null)
                UpdatePackageDetails();
            UpdatePreflight();
        }
    }

    private void PreflightButton_Click(object sender, RoutedEventArgs e)
    {
        UpdatePreflight();
        var result = PixelOtaPreflight.Evaluate(_snapshot, _package);
        AddLog("Compatibility preflight: " + result.Status + " (no write permitted)");
    }

    private void ClearLogsButton_Click(object sender, RoutedEventArgs e)
    {
        _history.Clear();
        LogsText.Text = "";
    }

    private void UpdateDeviceDetails()
    {
        if (DeviceSummary is null)
            return;
        var device = _snapshot?.Devices.Count == 1 ? _snapshot.Devices[0] : null;
        DeviceSummary.Text = _snapshot is null ? T("unscanned") :
            _snapshot.Devices.Count == 0 ? T("notFound") :
            T("devices") + ": " + _snapshot.Devices.Count;
        ModelValue.Text = string.IsNullOrWhiteSpace(device?.Model) ? "—" : device.Model;
        CodeValue.Text = string.IsNullOrWhiteSpace(device?.Codename) ? "—" : device.Codename;
        StateValue.Text = device is null ? "—" : device.Transport + " / " + device.State;
        VersionValue.Text = string.IsNullOrWhiteSpace(device?.AndroidVersion) ? "—" : device.AndroidVersion;
        ToolStatus.Text = T("tools") + ": " +
            (_snapshot is null ? "—" :
                "ADB " + (_snapshot.Adb.Available ? "✓" : "✕") +
                "  ·  Fastboot " + (_snapshot.Fastboot.Available ? "✓" : "✕"));
    }

    private void UpdatePackageDetails()
    {
        if (PackageName is null)
            return;
        PackageName.Text = _package is null ? T("noPackage") : System.IO.Path.GetFileName(_package.Path);
        PackageCode.Text = T("target") + ": " + (_package?.DeviceCodename ?? "—");
        PackageType.Text = T("otaType") + ": " + (_package?.OtaType ?? "—");
        PackageBuild.Text = T("build") + ": " + (_package?.Build ?? "—");
        PackageHash.Text = "SHA-256: " + (_package?.Sha256 ?? "—");
    }

    private void UpdatePreflight()
    {
        if (PreflightResult is null)
            return;
        var assessment = PixelOtaPreflight.Evaluate(_snapshot, _package);
        PreflightResult.Text = T(assessment.Status.ToString());
    }


    private void UpdateSideloadButton()
    {
        if (StartSideloadButton is null) return;
        StartSideloadButton.IsEnabled = _otaVerified && _trustedCatalog is not null &&
            _snapshot is not null && _package is not null &&
            PixelOtaPreflight.Evaluate(_snapshot, _package).IsCompatibleForReview &&
            BackupConsent.IsChecked == true && TermsConsent.IsChecked == true &&
            RecoveryConsent.IsChecked == true;
    }

    private void OtaConsentChanged(object sender, RoutedEventArgs e) => UpdateSideloadButton();

    private async void VerifyOfficial_Click(object sender, RoutedEventArgs e)
    {
        _otaVerified = false;
        _trustedCatalog = null;
        UpdateSideloadButton();
        if (_package is null)
        {
            OfficialResult.Text = T("selectFirst");
            return;
        }

        VerifyOfficialButton.IsEnabled = false;
        OfficialProgress.IsActive = true;
        try
        {
            var catalog = await GoogleFirmwareCatalog.FetchAsync(GoogleImageType.Ota);
            var result = await GoogleFirmwareVerifier.VerifyAsync(_package.Path, GoogleImageType.Ota, catalog);
            _otaVerified = result.Verified;
            if (result.Verified) _trustedCatalog = catalog;
            OfficialResult.Text = result.Verified ? T("officialVerified") : T("officialFailed") + " " + result.Explanation;
            AddLog("Official OTA SHA-256 verification: " + result.Verified);
        }
        catch (Exception ex)
        {
            OfficialResult.Text = T("officialFailed") + " " + ex.Message;
            AddLog("Google catalog fetch/verify failed: " + ex.Message);
        }
        finally
        {
            VerifyOfficialButton.IsEnabled = true;
            OfficialProgress.IsActive = false;
            UpdateSideloadButton();
        }
    }

    private async void StartSideload_Click(object sender, RoutedEventArgs e)
    {
        UpdateSideloadButton();
        if (StartSideloadButton.IsEnabled == false || _snapshot is null ||
            _package is null || _trustedCatalog is null) return;
        var dialog = new ContentDialog
        {
            XamlRoot = (Content as FrameworkElement)!.XamlRoot,
            Title = T("confirmTitle"),
            Content = T("confirmMessage"),
            PrimaryButtonText = T("confirmStart"),
            CloseButtonText = T("confirmCancel"),
            DefaultButton = ContentDialogButton.Close
        };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;

        StartSideloadButton.IsEnabled = false;
        ScanButton.IsEnabled = false;
        PickPackageButton.IsEnabled = false;
        VerifyOfficialButton.IsEnabled = false;
        SideloadStatus.Text = T("otaRunning");
        try
        {
            var senderService = new PixelOtaTransfer(new SystemCommandRunner(), _devices.AdbExecutable);
            var result = await senderService.RunAsync(_snapshot, _package, _trustedCatalog,
                BackupConsent.IsChecked == true,
                TermsConsent.IsChecked == true,
                RecoveryConsent.IsChecked == true);
            SideloadStatus.Text = result.Detail;
            AddLog("OTA transfer result: " + result.Stage + " / " + result.Detail);
        }
        catch (Exception ex)
        {
            SideloadStatus.Text = T("otaFailed") + " " + ex.Message;
            AddLog("OTA exception: " + ex.Message);
        }
        finally
        {
            ScanButton.IsEnabled = true;
            PickPackageButton.IsEnabled = true;
            VerifyOfficialButton.IsEnabled = true;
            // Never permit a second write using a stale approval or possibly changed device state.
            _otaVerified = false;
            _trustedCatalog = null;
            BackupConsent.IsChecked = false;
            RecoveryConsent.IsChecked = false;
            UpdateSideloadButton();
        }
    }

    private async void PickFactory_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker();
        picker.FileTypeFilter.Add(".zip");
        WinRT.Interop.InitializeWithWindow.Initialize(
            picker, WinRT.Interop.WindowNative.GetWindowHandle(this));
        var file = await picker.PickSingleFileAsync();
        if (file is null) return;
        PickFactoryButton.IsEnabled = false;
        FactoryResult.Text = T("factoryChecking");
        try
        {
            var inspected = await FactoryImageInspector.InspectAsync(file.Path);
            var catalog = await GoogleFirmwareCatalog.FetchAsync(GoogleImageType.Factory);
            var verified = await GoogleFirmwareVerifier.VerifyAsync(file.Path, GoogleImageType.Factory, catalog);
            var codeMatch = _snapshot?.Devices.Count == 1 &&
                string.Equals(_snapshot.Devices[0].Codename, inspected.Codename, StringComparison.OrdinalIgnoreCase);
            FactoryResult.Text = string.Join("\n",
                T("factoryCode") + ": " + inspected.Codename,
                "SHA-256: " + inspected.Sha256,
                verified.Verified ? T("officialVerified") : T("officialFailed") + " " + verified.Explanation,
                codeMatch ? T("factoryMatch") : T("factoryMismatch"),
                T("rollbackTitle") + ": " + T(PixelDeviceCatalog.GetRollbackNotice(inspected.Codename) switch
                {
                    PixelDeviceCatalog.RollbackNotice.Pixel6Or8May2025 => "rollback2025",
                    PixelDeviceCatalog.RollbackNotice.Pixel10May2026 => "rollback2026",
                    _ => "rollbackUnknown"
                }),
                T("factoryWriteDisabled"));
            AddLog("Factory inspection: official=" + verified.Verified + ", device match=" + codeMatch);
        }
        catch (Exception ex)
        {
            FactoryResult.Text = T("factoryFailed") + " " + ex.Message;
            AddLog("Factory inspection error: " + ex.Message);
        }
        finally { PickFactoryButton.IsEnabled = true; }
    }

    private void AddLog(string message)
    {
        _history.Append('[').Append(DateTimeOffset.Now.ToString("HH:mm:ss")).Append("] ")
            .AppendLine(message);
        if (LogsText is not null)
            LogsText.Text = _history.ToString();
    }
}
