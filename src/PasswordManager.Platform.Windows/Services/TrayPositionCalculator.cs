using System;

namespace PasswordManager.Platform.Windows.Services;

public enum TaskbarEdge
{
    Bottom,
    Top,
    Right,
    Left
}

public readonly record struct DisplayBounds(double Left, double Top, double Right, double Bottom)
{
    public double Width => Right - Left;
    public double Height => Bottom - Top;
}

public readonly record struct WindowPoint(double X, double Y);

public readonly record struct WindowPlacementResult(double X, double Y, double Width, double Height);

/// <summary>
/// Mathematical positioning logic for tray mini vault flyout windows.
/// Ensures windows are correctly placed and clamped within monitor work areas across DPI scales.
/// </summary>
public static class TrayPositionCalculator
{
    public const double DefaultFlyoutWidth = 380;
    public const double DefaultFlyoutHeight = 520;
    public const double DefaultMargin = 12;

    /// <summary>
    /// Calculates the optimal top-left coordinate for the flyout window based on taskbar edge and monitor work area.
    /// </summary>
    public static WindowPlacementResult CalculatePlacement(
        DisplayBounds workArea,
        TaskbarEdge taskbarEdge,
        double windowWidth = DefaultFlyoutWidth,
        double windowHeight = DefaultFlyoutHeight,
        double margin = DefaultMargin)
    {
        double targetX;
        double targetY;

        switch (taskbarEdge)
        {
            case TaskbarEdge.Bottom:
                // Bottom-right corner (standard Windows 11 system tray notification area)
                targetX = workArea.Right - windowWidth - margin;
                targetY = workArea.Bottom - windowHeight - margin;
                break;

            case TaskbarEdge.Top:
                // Top-right corner
                targetX = workArea.Right - windowWidth - margin;
                targetY = workArea.Top + margin;
                break;

            case TaskbarEdge.Right:
                // Right taskbar: place next to right edge at bottom
                targetX = workArea.Right - windowWidth - margin;
                targetY = workArea.Bottom - windowHeight - margin;
                break;

            case TaskbarEdge.Left:
                // Left taskbar: place next to left edge at bottom
                targetX = workArea.Left + margin;
                targetY = workArea.Bottom - windowHeight - margin;
                break;

            default:
                targetX = workArea.Right - windowWidth - margin;
                targetY = workArea.Bottom - windowHeight - margin;
                break;
        }

        // Clamp boundaries strictly within the visible work area
        var minX = workArea.Left;
        var maxX = Math.Max(workArea.Left, workArea.Right - windowWidth);
        var minY = workArea.Top;
        var maxY = Math.Max(workArea.Top, workArea.Bottom - windowHeight);

        var clampedX = Math.Clamp(targetX, minX, maxX);
        var clampedY = Math.Clamp(targetY, minY, maxY);

        return new WindowPlacementResult(clampedX, clampedY, windowWidth, windowHeight);
    }

    /// <summary>
    /// Calculates placement targeted near specific tray click coordinates.
    /// </summary>
    public static WindowPlacementResult CalculatePlacementFromCursor(
        DisplayBounds workArea,
        WindowPoint cursorPoint,
        double windowWidth = DefaultFlyoutWidth,
        double windowHeight = DefaultFlyoutHeight,
        double margin = DefaultMargin)
    {
        // Align center of window with cursor X, and sit above cursor by default
        var targetX = cursorPoint.X - (windowWidth / 2.0);
        var targetY = cursorPoint.Y - windowHeight - margin;

        var minX = workArea.Left;
        var maxX = Math.Max(workArea.Left, workArea.Right - windowWidth);
        var minY = workArea.Top;
        var maxY = Math.Max(workArea.Top, workArea.Bottom - windowHeight);

        var clampedX = Math.Clamp(targetX, minX, maxX);
        var clampedY = Math.Clamp(targetY, minY, maxY);

        return new WindowPlacementResult(clampedX, clampedY, windowWidth, windowHeight);
    }
}
