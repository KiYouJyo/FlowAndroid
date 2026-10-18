using FlowAndroid.Core.Updates;
using Xunit;

namespace FlowAndroid.Core.Tests;

public sealed class GitHubReleaseSecurityTests
{
    [Theory]
    [InlineData("v0.1.0-rc.9", ReleaseCheckState.InvalidRelease)]
    [InlineData("v0.1.0", ReleaseCheckState.UpToDate)]
    [InlineData("v0.2.0", ReleaseCheckState.UpdateAvailable)]
    public void RejectsPrereleaseTagsAndOldProducts(string tag, ReleaseCheckState expected)
    {
        string name = "FlowAndroid-0.2.0-x64.msix";
        var release = new GitHubReleasePayload(tag,false,false,"update",[
            new GitHubAssetPayload(name,new Uri("https://github.com/KiYouJyo/FlowAndroid/releases/download/v0.2.0/"+name).ToString(),1000),
            new GitHubAssetPayload("SHA256SUMS.txt","https://github.com/KiYouJyo/FlowAndroid/releases/download/v0.2.0/SHA256SUMS.txt",85)
        ]);
        Assert.Equal(expected,GitHubReleaseClient.Select(release,new Version(0,1,0,100)).State);
    }

    [Fact]
    public void DuplicateAssetsAndMaliciousSourcesFailClosed()
    {
        var hashAsset = new GitHubAssetPayload("SHA256SUMS.txt",
            "https://github.com/KiYouJyo/FlowAndroid/releases/download/v0.2.0/SHA256SUMS.txt",85);
        var image = new GitHubAssetPayload("FlowAndroid-0.2.0-x64.msix",
            "https://github.com/KiYouJyo/FlowAndroid/releases/download/v0.2.0/FlowAndroid-0.2.0-x64.msix",100);
        var duplicate = new GitHubReleasePayload("v0.2.0",false,false,"",[image,image,hashAsset]);
        Assert.Equal(ReleaseCheckState.UnsupportedPackage,
            GitHubReleaseClient.Select(duplicate,new Version(0,1,0)).State);
        var malicious = image with { Url="https://mirror.example.org/a/FlowAndroid-0.2.0-x64.msix" };
        Assert.Equal(ReleaseCheckState.UnsupportedPackage,
            GitHubReleaseClient.Select(new("v0.2.0",false,false,"",[malicious,hashAsset]),new Version(0,1,0)).State);
    }
}
