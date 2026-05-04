using System.Diagnostics;
using System.Runtime.InteropServices;
using DesktopAiTranslator.Models;
using DesktopAiTranslator.Utils;
using WpfApplication = System.Windows.Application;
using WpfPoint = System.Windows.Point;

namespace DesktopAiTranslator.Services;

public sealed class MouseHookService : IDisposable
{
    private readonly AppSettings _settings;
    private readonly SelectionButtonService _selectionButtonService;
    private readonly LoggingService _logger;
    private readonly NativeMethods.LowLevelMouseProc _proc;
    private IntPtr _hookId;
    private WpfPoint _startPoint;
    private DateTimeOffset _downAt;
    private IntPtr _startHwnd;
    private bool _isDown;

    public bool Enabled
    {
        get => _settings.SelectionButton.Enabled;
        set => _settings.SelectionButton.Enabled = value;
    }

    public MouseHookService(AppSettings settings, SelectionButtonService selectionButtonService, LoggingService logger)
    {
        _settings = settings;
        _selectionButtonService = selectionButtonService;
        _logger = logger;
        _proc = HookCallback;
    }

    public void Start()
    {
        if (_hookId != IntPtr.Zero)
        {
            return;
        }

        using var process = Process.GetCurrentProcess();
        using var module = process.MainModule;
        var moduleHandle = NativeMethods.GetModuleHandle(module?.ModuleName);
        _hookId = NativeMethods.SetWindowsHookEx(NativeMethods.WH_MOUSE_LL, _proc, moduleHandle, 0);
        if (_hookId == IntPtr.Zero)
        {
            _logger.Error($"Mouse hook install failed. win32={Marshal.GetLastWin32Error()}");
        }
        else
        {
            _logger.Info("Mouse hook installed.");
        }
    }

    public void Stop()
    {
        if (_hookId == IntPtr.Zero)
        {
            return;
        }

        NativeMethods.UnhookWindowsHookEx(_hookId);
        _hookId = IntPtr.Zero;
        _logger.Info("Mouse hook uninstalled.");
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            try
            {
                var message = wParam.ToInt32();
                var hook = Marshal.PtrToStructure<NativeMethods.MSLLHOOKSTRUCT>(lParam);
                if (message == NativeMethods.WM_LBUTTONDOWN)
                {
                    _isDown = true;
                    _startPoint = new WpfPoint(hook.pt.X, hook.pt.Y);
                    _downAt = DateTimeOffset.Now;
                    _startHwnd = NativeMethods.GetForegroundWindow();
                }
                else if (message == NativeMethods.WM_LBUTTONUP && _isDown)
                {
                    _isDown = false;
                    var end = new WpfPoint(hook.pt.X, hook.pt.Y);
                    var info = new MouseDragInfo
                    {
                        StartPoint = _startPoint,
                        EndPoint = end,
                        DragDistance = Distance(_startPoint, end),
                        DragDuration = DateTimeOffset.Now - _downAt,
                        StartForegroundWindow = _startHwnd,
                        EndForegroundWindow = NativeMethods.GetForegroundWindow()
                    };

                    WpfApplication.Current.Dispatcher.BeginInvoke(async () => await HandleMouseUpAsync(info));
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Mouse hook callback failed.", ex);
            }
        }

        return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    private async Task HandleMouseUpAsync(MouseDragInfo info)
    {
        if (!Enabled || _selectionButtonService.IsPointerOverOwnWindow(info.EndPoint))
        {
            return;
        }

        if (info.DragDistance < _settings.SelectionButton.MinDragDistancePx)
        {
            return;
        }

        if (info.DragDuration.TotalMilliseconds < _settings.SelectionButton.MinDragDurationMs ||
            info.DragDuration.TotalMilliseconds > _settings.SelectionButton.MaxDragDurationMs)
        {
            return;
        }

        if (ProcessIsExcluded(info.EndForegroundWindow) || IsInTaskbarArea(info.EndPoint) || IsLikelyFullscreen(info.EndForegroundWindow))
        {
            return;
        }

        _logger.Info($"Selection-like drag detected. distance={info.DragDistance:F1}, durationMs={info.DragDuration.TotalMilliseconds:F0}");
        await _selectionButtonService.ShowButtonAsync(info.EndPoint, info.EndForegroundWindow);
    }

    private bool ProcessIsExcluded(IntPtr hwnd)
    {
        var name = ProcessHelper.GetProcessNameFromWindow(hwnd);
        return _settings.ExcludedProcesses.Any(p => string.Equals(p, name, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsInTaskbarArea(WpfPoint point)
    {
        var area = ScreenHelper.GetWorkAreaNear(point);
        return !area.Contains(point);
    }

    private bool IsLikelyFullscreen(IntPtr hwnd)
    {
        if (_settings.SelectionButton.AllowFullscreen || hwnd == IntPtr.Zero)
        {
            return false;
        }

        if (!NativeMethods.GetWindowRect(hwnd, out var rect))
        {
            return false;
        }

        var point = new WpfPoint(rect.Left + 1, rect.Top + 1);
        var area = ScreenHelper.GetMonitorBoundsNear(point);
        var width = rect.Right - rect.Left;
        var height = rect.Bottom - rect.Top;
        return rect.Left <= area.Left && rect.Top <= area.Top && width >= area.Width && height >= area.Height;
    }

    private static double Distance(WpfPoint a, WpfPoint b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    public void Dispose() => Stop();
}
