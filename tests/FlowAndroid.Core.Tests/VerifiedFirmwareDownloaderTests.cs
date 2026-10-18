using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using FlowAndroid.Core.Firmware;
using Xunit;

namespace FlowAndroid.Core.Tests;

public sealed class VerifiedFirmwareDownloaderTests
{
    private sealed class FakeHandler(byte[] bytes) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(bytes),
                RequestMessage = request
            });
    }

    private static OfficialFirmware Image(byte[] bytes) => new(
        GoogleImageType.Ota, "shiba-ota-bp3a.261005.003-abcdefgh.zip",
        Convert.ToHexStringLower(SHA256.HashData(bytes)),
        new Uri("https://dl.google.com/dl/android/aosp/shiba-ota-bp3a.261005.003-abcdefgh.zip"));

    [Fact]
    public async Task DownloadsVerifiesAndReusesExactlyMatchedCache()
    {
        byte[] bytes = [1,2,3,4,5,6,7];
        using var http = new HttpClient(new FakeHandler(bytes));
        var target = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            var downloader = new VerifiedFirmwareDownloader(http);
            var file = await downloader.DownloadAsync(Image(bytes), target);
            Assert.Equal(bytes, await File.ReadAllBytesAsync(file));
            Assert.Equal(file, await downloader.DownloadAsync(Image(bytes), target));
        }
        finally { if (Directory.Exists(target)) Directory.Delete(target, true); }
    }

    [Fact]
    public async Task RejectsCorruptDownloadAndRemovesPartial()
    {
        var real = new byte[] {1,2,3};
        using var http = new HttpClient(new FakeHandler([9,9,9]));
        var target = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            await Assert.ThrowsAsync<InvalidDataException>(() => new VerifiedFirmwareDownloader(http).DownloadAsync(Image(real), target));
            Assert.Empty(Directory.GetFiles(target));
        }
        finally { if (Directory.Exists(target)) Directory.Delete(target, true); }
    }
}
