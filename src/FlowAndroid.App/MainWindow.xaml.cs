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

    private void AddLog(string message)
    {
        _history.Append('[').Append(DateTimeOffset.Now.ToString("HH:mm:ss")).Append("] ")
            .AppendLine(message);
        if (LogsText is not null)
            LogsText.Text = _history.ToString();
    }
}
