using WpfPoint = System.Windows.Point;
using WpfRect = System.Windows.Rect;
using SystemParameters = System.Windows.SystemParameters;

namespace DesktopAiTranslator.Utils;

public static class ScreenHelper
{
    public static WpfRect GetWorkAreaNear(WpfPoint point)
    {
        if (TryGetMonitorInfo(point, out var info))
        {
            return new WpfRect(
                info.rcWork.Left,
                info.rcWork.Top,
                Math.Max(1, info.rcWork.Right - info.rcWork.Left),
                Math.Max(1, info.rcWork.Bottom - info.rcWork.Top));
        }

        return new WpfRect(SystemParameters.WorkArea.Left, SystemParameters.WorkArea.Top, SystemParameters.WorkArea.Width, SystemParameters.WorkArea.Height);
    }

    public static WpfRect GetMonitorBoundsNear(WpfPoint point)
    {
        if (TryGetMonitorInfo(point, out var info))
        {
            return new WpfRect(
                info.rcMonitor.Left,
                info.rcMonitor.Top,
                Math.Max(1, info.rcMonitor.Right - info.rcMonitor.Left),
                Math.Max(1, info.rcMonitor.Bottom - info.rcMonitor.Top));
        }

        return new WpfRect(0, 0, SystemParameters.PrimaryScreenWidth, SystemParameters.PrimaryScreenHeight);
    }

    public static WpfPoint KeepWindowOnScreen(WpfPoint desired, double width, double height)
    {
        var area = GetWorkAreaNear(desired);
        var x = Math.Min(Math.Max(desired.X, area.Left), area.Right - width);
        var y = Math.Min(Math.Max(desired.Y, area.Top), area.Bottom - height);
        return new WpfPoint(Math.Max(area.Left, x), Math.Max(area.Top, y));
    }

    private static bool TryGetMonitorInfo(WpfPoint point, out NativeMethods.MONITORINFO info)
    {
        var nativePoint = new NativeMethods.POINT { X = (int)point.X, Y = (int)point.Y };
        var monitor = NativeMethods.MonitorFromPoint(nativePoint, NativeMethods.MONITOR_DEFAULTTONEAREST);
        info = new NativeMethods.MONITORINFO { cbSize = System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.MONITORINFO>() };
        return monitor != IntPtr.Zero && NativeMethods.GetMonitorInfo(monitor, ref info);
    }
}
