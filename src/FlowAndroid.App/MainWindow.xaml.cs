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
    private IReadOnlyList<OfficialFirmware>? _wizardCatalog;
    private FirmwareChoice? _wizardChoice;
    private OfficialFirmware? _wizardSelected;
    private CancellationTokenSource? _downloadCancellation;
    private string? _postInstallExpected;
    private string? _postInstallSerial;
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
        AddLog("FlowAndroid 0.1.0 experimental preview. OTA transfer is write-capable after checks; factory flashing disabled.");
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
        WizardInfo.Title = T("wizardInfoTitle");
        WizardInfo.Message = T("wizardInfoBody");
        WizardStep1Title.Text = T("wizardStep1");
        WizardStep1Help.Text = T("wizardStep1Help");
        WizardScanButton.Content = T("wizardScan");
        WizardDemoButton.Content = T("wizardDemo");
        WizardStep2Title.Text = T("wizardStep2");
        WizardStep2Help.Text = T("wizardStep2Help");
        WizardFindButton.Content = T("wizardFind");
        WizardCandidateCombo.Header = T("wizardCandidates");
        WizardDownloadNote.Text = T("wizardDownloadNote");
        WizardDownloadButton.Content = T("wizardDownload");
        WizardCancelButton.Content = T("wizardCancel");
        WizardOfficialLink.Content = T("wizardOfficialLink");
        HomeDescription.Text = T("homeDesc");
        SafetyBanner.Title = T("bannerTitle");
        SafetyBanner.Message = T("bannerBody");
        DeviceHeading.Text = T("deviceTitle");
        ModelCaption.Text = T("model");
        CodeCaption.Text = T("code");
        StateCaption.Text = T("state");
        VersionCaption.Text = T("version");
        BatteryCaption.Text = T("battery");
        ScanButton.Content = T("scan");
        PlatformToolsLink.Content = T("platformToolsLink");
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
        VerifyPostInstallButton.Content = T("postInstallButton");
        if (SideloadStatus.Text.Length == 0) SideloadStatus.Text = T("sideloadHint");
        FactoryTitle.Text = T("factoryTitle");
        FactoryWarning.Text = T("factoryWarning");
        PickFactoryButton.Content = T("factoryPick");
        if (FactoryResult.Text.Length == 0) FactoryResult.Text = T("factoryPending");

        UpdateDeviceDetails();
        UpdatePackageDetails();
        UpdatePreflight();
        RefreshWizardDeviceStatus();
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
        // Any new scan invalidates the previous device-bound write approval.
        _otaVerified = false;
        _trustedCatalog = null;
        BackupConsent.IsChecked = false;
        RecoveryConsent.IsChecked = false;
        OfficialResult.Text = T("officialPending");
        UpdateSideloadButton();
        ScanButton.IsEnabled = false;
        WizardScanButton.IsEnabled = false;
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
            WizardScanButton.IsEnabled = true;
            ScanProgress.IsActive = false;
            _wizardChoice = null;
            _wizardSelected = null;
            _wizardCatalog = null;
            WizardCandidateCombo.Items.Clear();
            WizardCandidateCombo.IsEnabled = false;
            WizardDownloadButton.IsEnabled = false;
            RefreshWizardDeviceStatus();
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
            OfficialResult.Text = T("officialPending");
            RecoveryConsent.IsChecked = false;
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
        AddLog("Compatibility preflight: " + result.Status + " (not authorization to write)");
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
        BatteryValue.Text = device?.BatteryPercent is >= 0 and <= 100 ? device.BatteryPercent + "%" : "—";
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



    private void RefreshWizardDeviceStatus()
    {
        if (WizardDeviceStatus is null) return;
        var status = DeviceGuidanceEngine.Diagnose(_snapshot);
        WizardFindButton.IsEnabled = status.CanSearchFirmware;
        WizardDeviceStatus.Text = T("tip" + status.Problem);
        if (status.CanSearchFirmware && _snapshot!.Devices.Count == 1)
        {
            var d = _snapshot.Devices[0];
            WizardDeviceStatus.Text = T("wizardDeviceReady") + ": " + d.Model + " (" + d.Codename + "), " +
                T("battery") + ": " + d.BatteryPercent + "%";
        }
    }

    private void WizardScan_Click(object sender, RoutedEventArgs e)
    {
        ScanButton_Click(sender, e);
    }

    private void WizardDemo_Click(object sender, RoutedEventArgs e)
    {
        WizardDemoText.Visibility = WizardDemoText.Visibility == Visibility.Visible
            ? Visibility.Collapsed : Visibility.Visible;
        WizardDemoText.Text = T("wizardDemoSteps");
        // Never create fake ADB state or populate trust/catalog fields in demo mode.
        AddLog("Tutorial preview opened (no device commands or writes).");
    }

    private async void WizardFind_Click(object sender, RoutedEventArgs e)
    {
        if (_snapshot is not { HasOneOnlinePixel: true }) return;
        WizardFindButton.IsEnabled = false;
        WizardFindProgress.IsActive = true;
        _wizardChoice = null;
        _wizardSelected = null;
        _wizardCatalog = null;
        WizardCandidateCombo.Items.Clear();
        WizardCandidateCombo.IsEnabled = false;
        WizardDownloadButton.IsEnabled = false;
        try
        {
            var catalog = await GoogleFirmwareCatalog.FetchAsync(GoogleImageType.Ota);
            _wizardCatalog = catalog;
            var choice = GuidedFirmwarePlanner.Choose(_snapshot, catalog);
            _wizardChoice = choice;
            WizardChoiceStatus.Text = choice.Status switch
            {
                GuideChoiceStatus.ReadyToDownload => T("wizardSuggested"),
                GuideChoiceStatus.ReviewVariants => T("wizardVariantsBlocked"),
                GuideChoiceStatus.NoStableBuild => T("wizardNoStable"),
                _ => choice.Explanation
            };
            foreach (var image in choice.Candidates.Take(25))
            {
                WizardCandidateCombo.Items.Add(new ComboBoxItem
                {
                    Content = image.FileName,
                    Tag = image
                });
            }
            WizardCandidateCombo.IsEnabled = choice.Suggested is not null;
            if (choice.Suggested is not null)
            {
                for (int i = 0; i < WizardCandidateCombo.Items.Count; ++i)
                {
                    if ((WizardCandidateCombo.Items[i] as ComboBoxItem)?.Tag is OfficialFirmware item &&
                        item.FileName == choice.Suggested.FileName)
                    {
                        WizardCandidateCombo.SelectedIndex = i;
                        break;
                    }
                }
            }
            AddLog("Google OTA catalog read: " + catalog.Count +
                " entries; planner result: " + choice.Status);
        }
        catch (Exception ex)
        {
            WizardChoiceStatus.Text = T("wizardFindFailed") + " " + ex.Message;
            AddLog("Wizard catalog request failed: " + ex.Message);
        }
        finally
        {
            WizardFindButton.IsEnabled = DeviceGuidanceEngine.Diagnose(_snapshot).CanSearchFirmware;
            WizardFindProgress.IsActive = false;
            RefreshWizardDownloadEligibility();
        }
    }

    private void WizardCandidate_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _wizardSelected = (WizardCandidateCombo?.SelectedItem as ComboBoxItem)?.Tag as OfficialFirmware;
        RefreshWizardDownloadEligibility();
    }

    private void RefreshWizardDownloadEligibility()
    {
        if (WizardDownloadButton is null) return;
        WizardDownloadButton.IsEnabled = _downloadCancellation is null &&
            _wizardChoice?.Status == GuideChoiceStatus.ReadyToDownload &&
            _wizardChoice.Suggested is not null && _wizardSelected is not null &&
            string.Equals(_wizardSelected.FileName, _wizardChoice.Suggested.FileName, StringComparison.Ordinal) &&
            DeviceGuidanceEngine.Diagnose(_snapshot).CanSearchFirmware &&
            _wizardSelected.Codename.Equals(_snapshot.Devices[0].Codename, StringComparison.OrdinalIgnoreCase);
    }

    private async void WizardDownload_Click(object sender, RoutedEventArgs e)
    {
        var selected = _wizardSelected;
        var catalog = _wizardCatalog;
        if (selected is null || catalog is null || WizardDownloadButton.IsEnabled == false)
            return;
        _downloadCancellation = new CancellationTokenSource();
        _otaVerified = false;
        _trustedCatalog = null;
        _package = null;
        RecoveryConsent.IsChecked = false;
        BackupConsent.IsChecked = false;
        UpdatePackageDetails();
        UpdateSideloadButton();
        WizardCancelButton.Visibility = Visibility.Visible;
        WizardDownloadProgress.Visibility = Visibility.Visible;
        WizardDownloadProgress.IsIndeterminate = true;
        WizardDownloadStatus.Text = T("wizardDownloading");
        RefreshWizardDownloadEligibility();
        try
        {
            var root = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FlowAndroid", "OfficialOTA");
            using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(40) };
            var progress = new Progress<FirmwareDownloadProgress>(p =>
            {
                if (p.Percent is int percent)
                {
                    WizardDownloadProgress.IsIndeterminate = false;
                    WizardDownloadProgress.Value = percent;
                    WizardDownloadStatus.Text = T("wizardDownloading") + " " + percent + "%";
                }
            });
            var localPath = await new VerifiedFirmwareDownloader(http).DownloadAsync(
                selected, root, progress, _downloadCancellation.Token);
            var inspected = await _inspector.InspectAsync(localPath, _downloadCancellation.Token);
            var verified = await GoogleFirmwareVerifier.VerifyAsync(
                localPath, GoogleImageType.Ota, catalog, _downloadCancellation.Token);
            if (!verified.Verified || !inspected.DeviceCodename.Split(['|', ','])
                    .Any(code => code.Trim().Equals(selected.Codename, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidDataException("Downloaded ZIP identity or checksum did not verify.");
            _package = inspected;
            _trustedCatalog = catalog;
            _otaVerified = true;
            WizardDownloadProgress.IsIndeterminate = false;
            WizardDownloadProgress.Value = 100;
            WizardDownloadStatus.Text = T("wizardDownloadDone") + " " + selected.FileName;
            OfficialResult.Text = T("officialVerified");
            UpdatePackageDetails();
            UpdatePreflight();
            UpdateSideloadButton();
            AddLog("Official OTA downloaded and verified: " + selected.FileName);
        }
        catch (OperationCanceledException)
        {
            WizardDownloadStatus.Text = T("wizardCancelled");
        }
        catch (Exception ex)
        {
            WizardDownloadStatus.Text = T("wizardDownloadFailed") + " " + ex.Message;
            AddLog("OTA download error: " + ex.Message);
        }
        finally
        {
            _downloadCancellation.Dispose();
            _downloadCancellation = null;
            WizardCancelButton.Visibility = Visibility.Collapsed;
            WizardDownloadProgress.IsIndeterminate = false;
            RefreshWizardDownloadEligibility();
        }
    }

    private void WizardCancel_Click(object sender, RoutedEventArgs e) =>
        _downloadCancellation?.Cancel();

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
        VerifyPostInstallButton.IsEnabled = false;
        _postInstallExpected = null;
        _postInstallSerial = null;
        SideloadStatus.Text = T("otaRunning");
        try
        {
            var senderService = new PixelOtaTransfer(new SystemCommandRunner(), _devices.AdbExecutable);
            var result = await senderService.RunAsync(_snapshot, _package, _trustedCatalog,
                BackupConsent.IsChecked == true,
                TermsConsent.IsChecked == true,
                RecoveryConsent.IsChecked == true);
            SideloadStatus.Text = result.Detail;
            if (result.Stage == TransferStage.TransferDone && _package is not null && _snapshot?.Devices.Count == 1)
            {
                _postInstallExpected = _package.Build;
                _postInstallSerial = _snapshot.Devices[0].Serial;
                VerifyPostInstallButton.IsEnabled = true;
            }
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
            OfficialResult.Text = T("officialPending");
            BackupConsent.IsChecked = false;
            RecoveryConsent.IsChecked = false;
            UpdateSideloadButton();
        }
    }

    private async void VerifyPostInstall_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_postInstallExpected) || string.IsNullOrEmpty(_postInstallSerial))
            return;
        VerifyPostInstallButton.IsEnabled = false;
        try
        {
            var snapshot = await _devices.ScanAsync();
            var result = PixelPostInstallCheck.Evaluate(snapshot, _postInstallSerial, _postInstallExpected);
            SideloadStatus.Text = result.Success ? T("postInstallSuccess") : T("postInstallBlocked") + " " + result.Description;
            AddLog("Post-install verification: " + result.Status);
            // Verification button can be reused after manually addressing a not-booted state.
            if (!result.Success)
                VerifyPostInstallButton.IsEnabled = true;
        }
        catch (Exception ex)
        {
            SideloadStatus.Text = T("postInstallBlocked") + " " + ex.Message;
            VerifyPostInstallButton.IsEnabled = true;
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
