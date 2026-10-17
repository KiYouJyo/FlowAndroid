using FlowAndroid.Core.Devices;
using Xunit;

namespace FlowAndroid.Core.Tests;

public class PixelDeviceCatalogTests
{
    [Theory]
    [InlineData("shiba", "Pixel 8")]
    [InlineData("FRANKEL", "Pixel 10")]
    [InlineData("comet", "Pixel 9 Pro Fold")]
    public void KnownCodenamesHaveInformationalLabels(string code, string name)
        => Assert.Equal(name, PixelDeviceCatalog.GetModel(code));

    [Fact]
    public void UnknownCodenameNeverGetsConfidentModel()
        => Assert.Null(PixelDeviceCatalog.GetModel("unknown-future-device"));

    [Theory]
    [InlineData("shiba", PixelDeviceCatalog.RollbackNotice.Pixel6Or8May2025)]
    [InlineData("oriole", PixelDeviceCatalog.RollbackNotice.Pixel6Or8May2025)]
    [InlineData("frankel", PixelDeviceCatalog.RollbackNotice.Pixel10May2026)]
    [InlineData("tegu", PixelDeviceCatalog.RollbackNotice.Unknown)]
    public void RollbackAlertsAreRiskWarningsNotApproval(
        string codename, PixelDeviceCatalog.RollbackNotice expected)
        => Assert.Equal(expected, PixelDeviceCatalog.GetRollbackNotice(codename));
}
