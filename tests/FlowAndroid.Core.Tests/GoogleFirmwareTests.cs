using System.IO.Compression;
using FlowAndroid.Core.Firmware;
using FlowAndroid.Core.Devices;
using FlowAndroid.Core.Process;
using Xunit;

namespace FlowAndroid.Core.Tests;

public class GoogleFirmwareTests
{
    [Fact]
    public void GooglePageRejectsOtherHostsAndAmbiguousChecksums()
    {
        var h = new string('a', 64);
        var html = $"<tr><td><a href=\"https://dl.google.com/dl/android/aosp/shiba-ota-test-abcdef12.zip\">Download</a></td><td>{h}</td></tr>" +
            $"<tr><td><a href=\"https://evil.example/dl/android/aosp/shiba-ota-x.zip\">Download</a></td><td>{h}</td></tr>" +
            $"<tr><td><a href=\"https://dl.google.com/dl/android/aosp/shiba-ota-y.zip\">Download</a></td><td>{h} {h}</td></tr>";
        var item = Assert.Single(GoogleFirmwareCatalog.Parse(html, GoogleImageType.Ota));
        Assert.Equal("shiba", item.Codename);
    }

    private sealed class FakeRunner : ICommandRunner
    {
        public string Line = "one sideload";
        public int Count;
        public Task<CommandResult> RunAsync(string executable, IReadOnlyList<string> args,
            TimeSpan timeout, CancellationToken ct = default)
        {
            Count++;
            return Task.FromResult(args[0] == "devices"
                ? new CommandResult(0, "List of devices attached\n" + Line + "\n", "")
                : new CommandResult(0, "Transfer", ""));
        }
    }

    [Fact]
    public async Task SideloadRejectsDifferentSerialAndRequiresBackup()
    {
        var filename = "shiba-ota-test-" + Guid.NewGuid().ToString("N") + ".zip";
        var path = Path.Combine(Path.GetTempPath(), filename);
        using (var zip = ZipFile.Open(path, ZipArchiveMode.Create))
        {
            var entry = zip.CreateEntry("META-INF/com/android/metadata");
            using var writer = new StreamWriter(entry.Open());
            writer.Write("pre-device=shiba\nota-type=AB\npost-timestamp=1800000100");
        }
        try
        {
            var ota = await new OtaPackageInspector().InspectAsync(path);
            var trusted = new OfficialFirmware(GoogleImageType.Ota, filename, ota.Sha256,
                new Uri("https://dl.google.com/dl/android/aosp/" + filename));
            var snapshot = new DeviceSnapshot(new(true, "ADB"), new(true, "Fastboot"),
                [new AndroidDevice("one", "adb", "device", "shiba", "Pixel 8", "Google", "16", 1800000000, 75)]);
            var runner = new FakeRunner { Line = "wrong sideload" };
            var coordinator = new PixelOtaTransfer(runner);
            var result = await coordinator.RunAsync(snapshot, ota, [trusted], true, true, true);
            Assert.Equal(TransferStage.Blocked, result.Stage);
            Assert.Equal(1, runner.Count);
            var denied = await coordinator.RunAsync(snapshot, ota, [trusted], false, true, true);
            Assert.Equal(TransferStage.Blocked, denied.Stage);
            Assert.Equal(1, runner.Count);
            runner.Line = "one sideload";
            var done = await coordinator.RunAsync(snapshot, ota, [trusted], true, true, true);
            Assert.Equal(TransferStage.TransferDone, done.Stage);
            Assert.Equal(3, runner.Count);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task FactoryInspectorReadsOnlyMetadata()
    {
        var path = Path.Combine(Path.GetTempPath(), "shiba-sample-factory-abcdef12.zip");
        using (var zip = ZipFile.Open(path, ZipArchiveMode.Create))
        {
            zip.CreateEntry("shiba-sample/flash-all.bat");
            zip.CreateEntry("shiba-sample/image-shiba-sample.zip");
        }
        try
        {
            var item = await FactoryImageInspector.InspectAsync(path);
            Assert.Equal("shiba", item.Codename);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task ChecksumTamperingIsRejectedBeforeAdbCommands()
    {
        var filename = "shiba-ota-test-" + Guid.NewGuid().ToString("N") + ".zip";
        var path = Path.Combine(Path.GetTempPath(), filename);
        using (var zip = ZipFile.Open(path, ZipArchiveMode.Create))
        {
            var e = zip.CreateEntry("META-INF/com/android/metadata");
            using var writer = new StreamWriter(e.Open());
            writer.Write("pre-device=shiba\nota-type=AB\npost-timestamp=1800000100");
        }
        try
        {
            var package = await new OtaPackageInspector().InspectAsync(path);
            var official = new OfficialFirmware(GoogleImageType.Ota, filename,
                package.Sha256, new Uri("https://dl.google.com/dl/android/aosp/" + filename));
            await File.AppendAllTextAsync(path, "corrupted");
            var device = new DeviceSnapshot(new(true, "adb"), new(true, "fastboot"),
                [new AndroidDevice("one", "adb", "device", "shiba", "Pixel 8", "Google", "16", 1800000000, 75)]);
            var fake = new FakeRunner();
            var result = await new PixelOtaTransfer(fake).RunAsync(device, package, [official], true, true, true);
            Assert.Equal(TransferStage.Blocked, result.Stage);
            Assert.Equal(0, fake.Count);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task OlderOtaBuildIsBlockedBeforeRecoveryTransport()
    {
        var filename = "shiba-ota-test-" + Guid.NewGuid().ToString("N") + ".zip";
        var path = Path.Combine(Path.GetTempPath(), filename);
        using (var zip = ZipFile.Open(path, ZipArchiveMode.Create))
        {
            var e = zip.CreateEntry("META-INF/com/android/metadata");
            using var writer = new StreamWriter(e.Open());
            writer.Write("pre-device=shiba\nota-type=AB\npost-timestamp=1700000000");
        }
        try
        {
            var package = await new OtaPackageInspector().InspectAsync(path);
            var official = new OfficialFirmware(GoogleImageType.Ota, filename,
                package.Sha256, new Uri("https://dl.google.com/dl/android/aosp/" + filename));
            var device = new DeviceSnapshot(new(true, "adb"), new(true, "fastboot"),
                [new AndroidDevice("one", "adb", "device", "shiba", "Pixel 8", "Google", "16", 1800000000, 75)]);
            var fake = new FakeRunner();
            var result = await new PixelOtaTransfer(fake).RunAsync(device, package, [official], true, true, true);
            Assert.Equal(TransferStage.Blocked, result.Stage);
            Assert.Equal(0, fake.Count);
        }
        finally { File.Delete(path); }
    }

}
