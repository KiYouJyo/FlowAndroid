using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Xml;
using Windows.ApplicationModel;
using Windows.Management.Deployment;

namespace FlowAndroid.Updates;

/// <summary>
/// Same Windows Restart Manager + ForceApplicationShutdown update handoff used in UrbanPlanToolbox.
/// Trust is tied to the *installed* package name/publisher, not a hardcoded signing certificate.
/// </summary>
public static class WindowsPackageUpdater
{
    private static readonly Guid WintrustAction = new("00AAC56B-CD44-11D0-8CC2-00C04FC295EE");

    public static bool IsPackaged
    {
        get { try { _ = Package.Current.Id.FullName; return true; } catch { return false; } }
    }

    public static Version InstalledVersion
    {
        get
        {
            try
            {
                var v = Package.Current.Id.Version;
                return new Version(v.Major, v.Minor, v.Build, v.Revision);
            }
            catch { return new Version(0, 1, 0, 0); }
        }
    }

    public static string DisplayVersion => InstalledVersion.ToString(4);

    public static void VerifyUpdatePackage(string path)
    {
        if (!IsPackaged) throw new InvalidOperationException(
            "Install the packaged MSIX build to enable in-app updates.");
        if (!File.Exists(path)) throw new FileNotFoundException("Downloaded MSIX was not found.", path);

        // Use the same WinVerifyTrust implementation as the proven UrbanPlanToolbox updater.
        // This rejects bad Windows package signatures instead of merely trusting SHA256SUMS.
        var result = VerifyWinTrust(path);
        if (result != 0)
            throw new InvalidDataException($"Windows package signature verification failed: 0x{result:X8}");

        using var zip = ZipFile.OpenRead(path);
        var manifest = zip.GetEntry("AppxManifest.xml")
            ?? throw new InvalidDataException("MSIX AppxManifest.xml is missing.");
        if (manifest.Length > 1024 * 1024)
            throw new InvalidDataException("MSIX manifest unexpectedly large.");
        using var reader = XmlReader.Create(manifest.Open(), new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = 1024 * 1024
        });
        var document = new XmlDocument { XmlResolver = null };
        document.Load(reader);
        var ns = new XmlNamespaceManager(document.NameTable);
        ns.AddNamespace("m", "http://schemas.microsoft.com/appx/manifest/foundation/windows10");
        var id = document.SelectSingleNode("/m:Package/m:Identity", ns) as XmlElement
            ?? throw new InvalidDataException("No signed MSIX identity found.");
        var current = Package.Current.Id;
        if (!string.Equals(id.GetAttribute("Name"), current.Name, StringComparison.Ordinal) ||
            !string.Equals(id.GetAttribute("Publisher"), current.Publisher, StringComparison.Ordinal))
            throw new InvalidDataException("MSIX package identity/publisher does not match this installed application.");
        if (!string.Equals(id.GetAttribute("ProcessorArchitecture"), "x64", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Unexpected MSIX architecture.");
        if (!Version.TryParse(id.GetAttribute("Version"), out var remote) || remote <= InstalledVersion)
            throw new InvalidDataException("MSIX is not newer than the installed package.");
    }

    public static async Task InstallAndRestartAsync(string path, CancellationToken cancellationToken = default)
    {
        VerifyUpdatePackage(path);
        cancellationToken.ThrowIfCancellationRequested();
        int registration = RegisterApplicationRestart(null, 0);
        if (registration != 0)
            throw new COMException("Windows could not register app restart.", registration);

        // ForceApplicationShutdown is essential: waiting for ordinary registration
        // often races a running WinUI 3 app and leaves the old version installed.
        var manager = new PackageManager();
        var operation = manager.AddPackageAsync(
            new Uri(Path.GetFullPath(path)), null, DeploymentOptions.ForceApplicationShutdown);
        var outcome = await operation.AsTask(cancellationToken);
        if (!outcome.IsRegistered)
            throw new InvalidOperationException("Windows MSIX registration failed: " + outcome.ErrorText);
        // On supported Windows versions the OS restarts the registered app after shutdown.
        // Do not start a second installer process or re-enter this path automatically.
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern int RegisterApplicationRestart(string? commandLine, uint flags);

    private static int VerifyWinTrust(string file)
    {
        var fileInfo = new WinTrustFileInfo(file);
        var pointer = Marshal.AllocHGlobal(Marshal.SizeOf<WinTrustFileInfo>());
        Marshal.StructureToPtr(fileInfo, pointer, false);
        var data = new WinTrustData(pointer);
        try
        {
            return WinVerifyTrust(IntPtr.Zero, WintrustAction, ref data);
        }
        finally
        {
            data.StateAction = 2;
            WinVerifyTrust(IntPtr.Zero, WintrustAction, ref data);
            Marshal.DestroyStructure<WinTrustFileInfo>(pointer);
            Marshal.FreeHGlobal(pointer);
        }
    }

    [DllImport("wintrust.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
    private static extern int WinVerifyTrust(IntPtr hwnd, [In] Guid action, ref WinTrustData data);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WinTrustFileInfo
    {
        public uint Size;
        [MarshalAs(UnmanagedType.LPWStr)] public string FilePath;
        public IntPtr FileHandle;
        public IntPtr KnownSubject;
        public WinTrustFileInfo(string file)
        {
            Size = (uint)Marshal.SizeOf<WinTrustFileInfo>();
            FilePath = file; FileHandle = IntPtr.Zero; KnownSubject = IntPtr.Zero;
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WinTrustData
    {
        public uint Size;
        public IntPtr CallbackData;
        public IntPtr ClientData;
        public uint UiChoice;
        public uint RevocationChecks;
        public uint UnionChoice;
        public IntPtr File;
        public uint StateAction;
        public IntPtr StateData;
        [MarshalAs(UnmanagedType.LPWStr)] public string? UrlReference;
        public uint ProviderFlags;
        public uint UiContext;
        public IntPtr SignatureSettings;
        public WinTrustData(IntPtr pointer)
        {
            Size = (uint)Marshal.SizeOf<WinTrustData>();
            CallbackData = IntPtr.Zero; ClientData = IntPtr.Zero;
            UiChoice = 2; RevocationChecks = 0; UnionChoice = 1;
            File = pointer; StateAction = 1; StateData = IntPtr.Zero;
            UrlReference = null; ProviderFlags = 0x10; UiContext = 0;
            SignatureSettings = IntPtr.Zero;
        }
    }
}
