using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FlowAndroid.Core.Updates;
using Xunit;

namespace FlowAndroid.Core.Tests;

public sealed class GitHubReleaseClientTests
{
    private static readonly string Name = "FlowAndroid-0.2.0-x64.msix";
    private static GitHubReleasePayload Release(string tag="v0.2.0", bool prerelease=false,
        string? packageHost = null) => new(tag, prerelease, false, "Release notes",
            [
                new(Name, (packageHost??"https://github.com")+"/KiYouJyo/FlowAndroid/releases/download/v0.2.0/"+Name, 3),
                new("SHA256SUMS.txt", "https://github.com/KiYouJyo/FlowAndroid/releases/download/v0.2.0/SHA256SUMS.txt", 85)
            ]);
    [Fact]
    public void StableReleaseMustBeNewerWithExactAssets()
    {
        Assert.Equal(ReleaseCheckState.UpdateAvailable, GitHubReleaseClient.Select(Release(), new Version(0,1,0,800)).State);
        Assert.Equal(ReleaseCheckState.UpToDate, GitHubReleaseClient.Select(Release(), new Version(0,2,0,0)).State);
        Assert.Equal(ReleaseCheckState.InvalidRelease, GitHubReleaseClient.Select(Release(prerelease:true), new Version(0,1,0)).State);
        Assert.Equal(ReleaseCheckState.UnsupportedPackage, GitHubReleaseClient.Select(Release(packageHost:"https://evil.example"), new Version(0,1,0)).State);
        Assert.Equal(ReleaseCheckState.InvalidRelease, GitHubReleaseClient.Select(Release(tag:"v0.2.0-rc.1"),new Version(0,1,0)).State);
    }
    [Fact]
    public void ChecksumParserRejectsDuplicatesAndWrongFilename()
    {
        string hash=new string('a',64);
        Assert.Equal(hash.ToUpperInvariant(), GitHubReleaseClient.ParseChecksum(hash+"  "+Name, Name));
        Assert.Null(GitHubReleaseClient.ParseChecksum(hash+"  other.msix",Name));
        Assert.Null(GitHubReleaseClient.ParseChecksum(hash+"  "+Name+"\n"+hash+"  "+Name,Name));
    }

    private sealed class FakeHandler(Dictionary<string,byte[]> data):HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage req,CancellationToken ct)
        {
            var uri=req.RequestUri!.AbsoluteUri;
            return Task.FromResult(data.TryGetValue(uri,out var bytes)
                ? new HttpResponseMessage(HttpStatusCode.OK){Content=new ByteArrayContent(bytes)}
                : new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }
    [Fact]
    public async Task CheckDownloadAndHashWithoutNetwork()
    {
        var bytes=new byte[]{1,2,3};
        var rel=Release();
        var checkHash=Convert.ToHexString(SHA256.HashData(bytes));
        var manifest=Encoding.UTF8.GetBytes(checkHash+"  "+Name+"\n");
        var json=JsonSerializer.Serialize(new {
            tag_name="v0.2.0", prerelease=false, draft=false, body="Release notes",
            assets=new[] {
                new {name=Name,browser_download_url=rel.Assets![0].Url,size=3},
                new {name="SHA256SUMS.txt",browser_download_url=rel.Assets[1].Url,size=85}
            }
        });
        using var http=new HttpClient(new FakeHandler(new(){
            [GitHubReleaseClient.LatestApi]=Encoding.UTF8.GetBytes(json),
            [rel.Assets[0].Url!]=bytes,[rel.Assets[1].Url!]=manifest
        }));
        var updater=new GitHubReleaseClient(http);
        var check=await updater.CheckAsync(new Version(0,1,0));
        Assert.Equal(ReleaseCheckState.UpdateAvailable,check.State);
        string dir=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N"));
        try{
            string path=await updater.DownloadVerifiedAsync(check.Update!,dir);
            Assert.Equal(bytes,await File.ReadAllBytesAsync(path));
        }finally{if(Directory.Exists(dir)) Directory.Delete(dir,true);}
    }

    [Fact]
    public async Task BadChecksumNeverPublishesMsix()
    {
        var bytes=new byte[]{1,2,3};
        var rel=Release();
        using var http=new HttpClient(new FakeHandler(new(){
            [rel.Assets![0].Url!]=bytes,
            [rel.Assets[1].Url!]=Encoding.UTF8.GetBytes(new string('f',64)+"  "+Name)
        }));
        string dir=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N"));
        try{
            await Assert.ThrowsAsync<InvalidDataException>(()=>new GitHubReleaseClient(http).DownloadVerifiedAsync(
                new UpdateRelease(new Version(0,2,0),"v0.2.0","",new(Name,new Uri(rel.Assets[0].Url!),3),
                    new("SHA256SUMS.txt",new Uri(rel.Assets[1].Url!),85)),dir));
            Assert.Empty(Directory.GetFiles(dir));
        }finally{if(Directory.Exists(dir)) Directory.Delete(dir,true);}
    }
}
