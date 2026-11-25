using VOIDOverlay.Models;

namespace VOIDOverlay.Services;

public readonly record struct PixelBounds(double Left, double Top, double Right, double Bottom);
public readonly record struct OverlayLocation(double Left, double Top);

public static class OverlayPositionCalculator
{
    private const double PresetInsetDip = 10;

    public static PixelBounds SelectEdgeBounds(
        PixelBounds fullScreenBounds,
        PixelBounds workAreaBounds,
        OverlayEdgeReference edgeReference) =>
        edgeReference switch
        {
            OverlayEdgeReference.FullScreen => fullScreenBounds,
            OverlayEdgeReference.WorkArea => workAreaBounds,
            _ => throw new ArgumentOutOfRangeException(nameof(edgeReference))
        };

    public static OverlayLocation Calculate(
        PixelBounds monitorBounds,
        PixelBounds usableBounds,
        double dpiScaleX,
        double dpiScaleY,
        double overlayWidthDip,
        double overlayHeightDip,
        double horizontalOffsetDip,
        double verticalOffsetDip)
    {
        ValidateDpi(dpiScaleX, dpiScaleY);
        ValidateOffsets(horizontalOffsetDip, verticalOffsetDip);

        var monitor = ToDipBounds(monitorBounds, dpiScaleX, dpiScaleY);
        var usable = ToDipBounds(usableBounds, dpiScaleX, dpiScaleY);
        var range = GetOffsetRange(monitor, usable, overlayWidthDip, overlayHeightDip);
        var offsetX = Math.Clamp(horizontalOffsetDip, range.MinimumX, range.MaximumX);
        var offsetY = Math.Clamp(verticalOffsetDip, range.MinimumY, range.MaximumY);
        var centerX = (monitor.Left + monitor.Right) / 2;
        var centerY = (monitor.Top + monitor.Bottom) / 2;
        var left = centerX + offsetX - overlayWidthDip / 2;
        var top = centerY + offsetY - overlayHeightDip / 2;

        return new(
            Math.Clamp(left, monitor.Left, Math.Max(monitor.Left, monitor.Right - overlayWidthDip)),
            Math.Clamp(top, monitor.Top, Math.Max(monitor.Top, monitor.Bottom - overlayHeightDip)));
    }

    public static (double MinimumX, double MaximumX, double MinimumY, double MaximumY) GetOffsetRange(
        PixelBounds monitorBounds,
        PixelBounds usableBounds,
        double dpiScaleX,
        double dpiScaleY,
        double overlayWidthDip,
        double overlayHeightDip)
    {
        ValidateDpi(dpiScaleX, dpiScaleY);
        return GetOffsetRange(
            ToDipBounds(monitorBounds, dpiScaleX, dpiScaleY),
            ToDipBounds(usableBounds, dpiScaleX, dpiScaleY),
            overlayWidthDip,
            overlayHeightDip);
    }

    public static (double X, double Y) GetPresetOffsets(
        PixelBounds monitorBounds,
        PixelBounds usableBounds,
        double dpiScaleX,
        double dpiScaleY,
        double overlayWidthDip,
        double overlayHeightDip,
        OverlayPosition preset)
    {
        var range = GetOffsetRange(
            monitorBounds,
            usableBounds,
            dpiScaleX,
            dpiScaleY,
            overlayWidthDip,
            overlayHeightDip);
        var x = preset switch
        {
            OverlayPosition.TopLeft or OverlayPosition.CenterLeft or OverlayPosition.BottomLeft =>
                range.MinimumX + PresetInsetDip,
            OverlayPosition.TopCenter or OverlayPosition.Center or OverlayPosition.BottomCenter => 0,
            OverlayPosition.TopRight or OverlayPosition.CenterRight or OverlayPosition.BottomRight =>
                range.MaximumX - PresetInsetDip,
            _ => throw new ArgumentOutOfRangeException(nameof(preset))
        };
        var y = preset switch
        {
            OverlayPosition.TopLeft or OverlayPosition.TopCenter or OverlayPosition.TopRight =>
                range.MinimumY + PresetInsetDip,
            OverlayPosition.CenterLeft or OverlayPosition.Center or OverlayPosition.CenterRight => 0,
            OverlayPosition.BottomLeft or OverlayPosition.BottomCenter or OverlayPosition.BottomRight =>
                range.MaximumY - PresetInsetDip,
            _ => throw new ArgumentOutOfRangeException(nameof(preset))
        };

        return (Math.Clamp(x, range.MinimumX, range.MaximumX), Math.Clamp(y, range.MinimumY, range.MaximumY));
    }

    public static (double X, double Y) MigrateLegacyOffsets(
        PixelBounds monitorBounds,
        PixelBounds usableBounds,
        double dpiScaleX,
        double dpiScaleY,
        double overlayWidthDip,
        double overlayHeightDip,
        double legacyOffsetX,
        double legacyOffsetY,
        OverlayPosition legacyPosition)
    {
        ValidateDpi(dpiScaleX, dpiScaleY);
        ValidateOffsets(legacyOffsetX, legacyOffsetY);

        var monitor = ToDipBounds(monitorBounds, dpiScaleX, dpiScaleY);
        var usable = ToDipBounds(usableBounds, dpiScaleX, dpiScaleY);
        var left = legacyPosition switch
        {
            OverlayPosition.TopLeft or OverlayPosition.BottomLeft or OverlayPosition.CenterLeft =>
                usable.Left + PresetInsetDip,
            OverlayPosition.TopCenter or OverlayPosition.BottomCenter or OverlayPosition.Center =>
                usable.Left + (usable.Right - usable.Left - overlayWidthDip) / 2,
            _ => usable.Right - overlayWidthDip - PresetInsetDip
        } + legacyOffsetX;
        var top = legacyPosition switch
        {
            OverlayPosition.TopLeft or OverlayPosition.TopCenter or OverlayPosition.TopRight =>
                usable.Top + PresetInsetDip,
            OverlayPosition.CenterLeft or OverlayPosition.Center or OverlayPosition.CenterRight =>
                usable.Top + (usable.Bottom - usable.Top - overlayHeightDip) / 2,
            _ => usable.Bottom - overlayHeightDip - PresetInsetDip
        } + legacyOffsetY;

        left = Math.Clamp(left, monitor.Left, Math.Max(monitor.Left, monitor.Right - overlayWidthDip));
        top = Math.Clamp(top, monitor.Top, Math.Max(monitor.Top, monitor.Bottom - overlayHeightDip));
        return (
            left + overlayWidthDip / 2 - (monitor.Left + monitor.Right) / 2,
            top + overlayHeightDip / 2 - (monitor.Top + monitor.Bottom) / 2);
    }

    private static (double MinimumX, double MaximumX, double MinimumY, double MaximumY) GetOffsetRange(
        PixelBounds monitor,
        PixelBounds usable,
        double overlayWidthDip,
        double overlayHeightDip)
    {
        var centerX = (monitor.Left + monitor.Right) / 2;
        var centerY = (monitor.Top + monitor.Bottom) / 2;
        var horizontalInset = Math.Min(overlayWidthDip, usable.Right - usable.Left) / 2;
        var verticalInset = Math.Min(overlayHeightDip, usable.Bottom - usable.Top) / 2;
        return (
            usable.Left + horizontalInset - centerX,
            usable.Right - horizontalInset - centerX,
            usable.Top + verticalInset - centerY,
            usable.Bottom - verticalInset - centerY);
    }

    private static PixelBounds ToDipBounds(PixelBounds bounds, double dpiScaleX, double dpiScaleY) =>
        new(
            bounds.Left / dpiScaleX,
            bounds.Top / dpiScaleY,
            bounds.Right / dpiScaleX,
            bounds.Bottom / dpiScaleY);

    private static void ValidateDpi(double dpiScaleX, double dpiScaleY)
    {
        if (!double.IsFinite(dpiScaleX) || dpiScaleX <= 0 ||
            !double.IsFinite(dpiScaleY) || dpiScaleY <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dpiScaleX), "DPI scales must be finite and positive.");
        }
    }

    private static void ValidateOffsets(double horizontalOffsetDip, double verticalOffsetDip)
    {
        if (!double.IsFinite(horizontalOffsetDip) || !double.IsFinite(verticalOffsetDip))
        {
            throw new ArgumentOutOfRangeException(nameof(horizontalOffsetDip), "Offsets must be finite values.");
        }
    }
}
