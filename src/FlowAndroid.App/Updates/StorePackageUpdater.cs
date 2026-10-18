using Windows.Services.Store;

namespace FlowAndroid.Updates;

/// <summary>Native Microsoft Store updates; never falls back to a GitHub package.</summary>
public sealed class StorePackageUpdater
{
    private readonly nint _windowHandle;
    private StoreContext? _context;
    private IReadOnlyList<StorePackageUpdate> _available = [];

    public StorePackageUpdater(nint windowHandle) => _windowHandle = windowHandle;

    private StoreContext GetContext()
    {
        if (_windowHandle == nint.Zero) throw new InvalidOperationException("Main window is not available.");
        if (_context is not null) return _context;
        var context = StoreContext.GetDefault();
        WinRT.Interop.InitializeWithWindow.Initialize(context, _windowHandle);
        return _context = context;
    }

    public async Task<(bool Available, string? Version)> CheckAsync()
    {
        _available = await GetContext().GetAppAndOptionalStorePackageUpdatesAsync();
        if (_available.Count == 0) return (false, null);
        // Store availability is authoritative; a hosted GitHub version must not override it.
        var versions = _available.Select(x => x.Package.Id.Version).ToArray();
        var newest = versions.OrderByDescending(v => new Version(v.Major, v.Minor, v.Build, v.Revision)).First();
        return (true, $"{newest.Major}.{newest.Minor}.{newest.Build}.{newest.Revision}");
    }

    public async Task<bool> DownloadAndInstallAsync(IProgress<double?>? progress = null)
    {
        if (_available.Count == 0) throw new InvalidOperationException("No Microsoft Store updates are available.");
        var operation = GetContext().RequestDownloadAndInstallStorePackageUpdatesAsync(_available);
        operation.Progress = (_, status) =>
        {
            var fraction = status.TotalDownloadProgress;
            progress?.Report(fraction is >= 0 and <= 1 ? fraction : null);
        };
        var result = await operation;
        return result.OverallState == StorePackageUpdateState.Completed;
    }
}
