using FlowAndroid.Core.Devices;
using FlowAndroid.Core.Firmware;
using Xunit;

namespace FlowAndroid.Core.Tests;

public sealed class GuidedFirmwarePlannerTests
{
    private static DeviceSnapshot Snapshot(string code = "shiba") => new(
        new(true,"adb"), new(true,"fastboot"),
        [new AndroidDevice("PIXEL-1","adb","device",code,"Pixel 8","Google","16",1800000000,90)]);

    private static OfficialFirmware Image(string name) => new(GoogleImageType.Ota,name,
        new string('a',64),new Uri("https://dl.google.com/dl/android/aosp/" + name));

    [Fact]
    public void RefusesAutomaticSelectionWhenRegionalVariantsShareNewestDate()
    {
        var result = GuidedFirmwarePlanner.Choose(Snapshot(),
        [
            Image("shiba-ota-bp3a.261005.003-aaaaaaaa.zip"),
            Image("shiba-ota-bp3a.261005.003.b1-bbbbbbbb.zip"),
            Image("shiba-ota-bp3a.260905.003-cccccccc.zip")
        ]);
        Assert.Equal(GuideChoiceStatus.ReviewVariants, result.Status);
        Assert.Null(result.Suggested);
        Assert.Equal(3, result.Candidates.Count);
    }

    [Fact]
    public void SingleProductionCandidateMayBeSuggestedButNotApplied()
    {
        var result = GuidedFirmwarePlanner.Choose(Snapshot(),
        [
            Image("shiba_beta-ota-bp3a.261005.003-aaaaaaaa.zip"),
            Image("husky-ota-bp3a.261005.003-bbbbbbbb.zip"),
            Image("shiba-ota-bp3a.261005.003-cccccccc.zip")
        ]);
        Assert.Equal(GuideChoiceStatus.ReadyToDownload, result.Status);
        Assert.Equal("shiba-ota-bp3a.261005.003-cccccccc.zip", result.Suggested?.FileName);
    }

    [Fact]
    public void NoDeviceAndUnknownDeviceNeverSuggestFirmware()
    {
        Assert.Null(GuidedFirmwarePlanner.Choose(null, []).Suggested);
        Assert.Equal(GuideChoiceStatus.UnsupportedDevice,
            GuidedFirmwarePlanner.Choose(Snapshot("unknownpixel"), [Image("unknownpixel-ota-bp3a.261005.003-aaaaaaaa.zip")]).Status);
    }
}
