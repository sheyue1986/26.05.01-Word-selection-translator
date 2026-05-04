using System.Diagnostics;
using DesktopAiTranslator.Utils;

namespace DesktopAiTranslator.Utils;

public static class ProcessHelper
{
    public static string GetProcessNameFromWindow(IntPtr hwnd)
    {
        try
        {
            if (hwnd == IntPtr.Zero)
            {
                return "";
            }

            NativeMethods.GetWindowThreadProcessId(hwnd, out var pid);
            return pid == 0 ? "" : Process.GetProcessById((int)pid).ProcessName + ".exe";
        }
        catch
        {
            return "";
        }
    }
}
