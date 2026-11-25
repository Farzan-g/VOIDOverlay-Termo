using VOIDOverlay.Models;
using VOIDOverlay.Services;

namespace VOIDOverlay.Tests;

public sealed class OverlayPositionCalculatorTests
{
    [Theory]
    [InlineData(1d, 1d)]
    [InlineData(1.5d, 1.25d)]
    public void ZeroOffsetsCenterOnTheSelectedDisplay(double scaleX, double scaleY)
    {
        var monitor = new PixelBounds(-1920, 0, 0, 1080);
        var usable = new PixelBounds(-1920, 0, 0, 1040);

        var location = Calculate(monitor, usable, scaleX, scaleY, 280, 44, 0, 0);

        Assert.Equal(-960 / scaleX - 140, location.Left, 6);
        Assert.Equal(540 / scaleY - 22, location.Top, 6);
    }

    [Theory]
    [InlineData(2d, 3d)]
    [InlineData(-2d, -3d)]
    public void OffsetsMoveRightAndDownOrLeftAndUp(double x, double y)
    {
        var monitor = new PixelBounds(0, 0, 1920, 1080);
        var usable = new PixelBounds(0, 0, 1920, 1040);
        var baseline = Calculate(monitor, usable, 1, 1, 280, 44, 0, 0);

        var location = Calculate(monitor, usable, 1, 1, 280, 44, x, y);

        Assert.Equal(x, location.Left - baseline.Left);
        Assert.Equal(y, location.Top - baseline.Top);
    }

    [Theory]
    [MemberData(nameof(AllPositions))]
    public void PositionPresetsAreConvenientOffsetsAroundCenter(OverlayPosition preset)
    {
        var monitor = new PixelBounds(0, 0, 1920, 1080);
        var usable = new PixelBounds(0, 0, 1920, 1040);
        var offsets = OverlayPositionCalculator.GetPresetOffsets(
            monitor, usable, 1, 1, 280, 44, preset);
        var location = Calculate(monitor, usable, 1, 1, 280, 44, offsets.X, offsets.Y);

        Assert.InRange(location.Left, 0, 1920 - 280);
        Assert.InRange(location.Top, 0, 1080 - 44);
        if (preset == OverlayPosition.Center)
        {
            Assert.Equal(0, offsets.X);
            Assert.Equal(0, offsets.Y);
        }
    }

    [Fact]
    public void PresetOffsetsRespectDpiAndWorkAreaBounds()
    {
        var monitor = new PixelBounds(1920, 0, 3840, 2160);
        var usable = new PixelBounds(1920, 0, 3840, 2050);

        var topLeft = OverlayPositionCalculator.GetPresetOffsets(
            monitor, usable, 2, 2, 300, 50, OverlayPosition.TopLeft);
        var bottomRight = OverlayPositionCalculator.GetPresetOffsets(
            monitor, usable, 2, 2, 300, 50, OverlayPosition.BottomRight);

        Assert.True(topLeft.X < 0);
        Assert.True(topLeft.Y < 0);
        Assert.True(bottomRight.X > 0);
        Assert.True(bottomRight.Y > 0);
    }

    [Fact]
    public void MigratesLegacyEdgeAnchoredPositionToEquivalentCenterOffsets()
    {
        var monitor = new PixelBounds(0, 0, 1920, 1080);
        var workArea = new PixelBounds(0, 0, 1920, 1040);
        var offsets = OverlayPositionCalculator.MigrateLegacyOffsets(
            monitor,
            workArea,
            1,
            1,
            280,
            44,
            0,
            0,
            OverlayPosition.TopCenter);
        var migrated = Calculate(monitor, workArea, 1, 1, 280, 44, offsets.X, offsets.Y);

        Assert.Equal((1920 - 280) / 2, migrated.Left);
        Assert.Equal(10, migrated.Top);
    }

    [Fact]
    public void ExtremeOffsetsRemainWithinSelectedMonitor()
    {
        var monitor = new PixelBounds(0, 0, 1000, 800);
        var usable = new PixelBounds(0, 0, 1000, 760);

        var location = Calculate(monitor, usable, 1, 1, 100, 40, 1000, 1000);

        Assert.Equal(900, location.Left);
        Assert.Equal(720, location.Top);
    }

    [Fact]
    public void RejectsInvalidDpiScaleAndNonFiniteOffsets()
    {
        var bounds = new PixelBounds(0, 0, 1000, 800);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Calculate(bounds, bounds, 0, 1, 180, 42, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Calculate(bounds, bounds, 1, 1, 180, 42, double.NaN, 0));
    }

    public static IEnumerable<object[]> AllPositions() =>
        Enum.GetValues<OverlayPosition>().Select(position => new object[] { position });

    private static OverlayLocation Calculate(
        PixelBounds monitor,
        PixelBounds usable,
        double scaleX,
        double scaleY,
        double width,
        double height,
        double x,
        double y) =>
        OverlayPositionCalculator.Calculate(monitor, usable, scaleX, scaleY, width, height, x, y);
}
