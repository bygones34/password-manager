using PasswordManager.Platform.Windows.Services;
using Xunit;

namespace PasswordManager.Windows.Tests;

public class TrayPositionCalculatorTests
{
    private readonly DisplayBounds _standardFhdWorkArea = new(0, 0, 1920, 1040); // 1080p with 40px taskbar at bottom

    [Fact]
    public void CalculatePlacement_BottomTaskbar_ShouldPlaceAtBottomRightCorner()
    {
        var result = TrayPositionCalculator.CalculatePlacement(
            _standardFhdWorkArea,
            TaskbarEdge.Bottom,
            windowWidth: 380,
            windowHeight: 520,
            margin: 12);

        // Expected X: 1920 - 380 - 12 = 1528
        // Expected Y: 1040 - 520 - 12 = 508
        Assert.Equal(1528, result.X);
        Assert.Equal(508, result.Y);
        Assert.Equal(380, result.Width);
        Assert.Equal(520, result.Height);
    }

    [Fact]
    public void CalculatePlacement_TopTaskbar_ShouldPlaceAtTopRightCorner()
    {
        var topWorkArea = new DisplayBounds(0, 40, 1920, 1080);
        var result = TrayPositionCalculator.CalculatePlacement(
            topWorkArea,
            TaskbarEdge.Top,
            windowWidth: 380,
            windowHeight: 520,
            margin: 12);

        // Expected X: 1920 - 380 - 12 = 1528
        // Expected Y: 40 + 12 = 52
        Assert.Equal(1528, result.X);
        Assert.Equal(52, result.Y);
    }

    [Fact]
    public void CalculatePlacement_SmallScreen_ShouldClampToZeroBoundary()
    {
        // Small screen 400x500, window 380x520
        var smallWorkArea = new DisplayBounds(0, 0, 400, 500);
        var result = TrayPositionCalculator.CalculatePlacement(
            smallWorkArea,
            TaskbarEdge.Bottom,
            windowWidth: 380,
            windowHeight: 520,
            margin: 12);

        // Window height 520 is larger than 500, should clamp to minimum Y (0)
        Assert.True(result.X >= 0);
        Assert.True(result.Y >= 0);
        Assert.Equal(0, result.Y);
    }

    [Fact]
    public void CalculatePlacementFromCursor_OutOfBoundsCursor_ShouldClampWithinWorkArea()
    {
        // Cursor at far right edge (1915, 1030)
        var cursor = new WindowPoint(1915, 1030);
        var result = TrayPositionCalculator.CalculatePlacementFromCursor(
            _standardFhdWorkArea,
            cursor,
            windowWidth: 380,
            windowHeight: 520,
            margin: 12);

        // Right edge (X + Width) must not exceed 1920
        Assert.True(result.X + result.Width <= _standardFhdWorkArea.Right);
        Assert.True(result.X >= _standardFhdWorkArea.Left);
        Assert.True(result.Y + result.Height <= _standardFhdWorkArea.Bottom);
        Assert.True(result.Y >= _standardFhdWorkArea.Top);
    }
}
