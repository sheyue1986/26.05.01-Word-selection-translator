using System.Runtime.InteropServices;
using DesktopAiTranslator.Models;
using DesktopAiTranslator.Utils;
using WpfApplication = System.Windows.Application;
using WpfClipboard = System.Windows.Clipboard;

namespace DesktopAiTranslator.Services;

public sealed class ClipboardTextCaptureService : ITextCaptureService
{
    private readonly AppSettings _settings;
    private readonly LoggingService _logger;

    public ClipboardTextCaptureService(AppSettings settings, LoggingService logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public async Task<TextCaptureResult> CaptureSelectedTextAsync(IntPtr targetWindow, System.Windows.Point selectionPoint)
    {
        string? oldText = null;
        var hadOldText = false;
        var sequenceBefore = NativeMethods.GetClipboardSequenceNumber();

        try
        {
            await WpfApplication.Current.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    hadOldText = WpfClipboard.ContainsText();
                    oldText = hadOldText ? WpfClipboard.GetText() : null;
                }
                catch (Exception ex)
                {
                    _logger.Warn($"Failed to read old clipboard text: {ex.Message}");
                }
            });

            var focusWindow = GetFocusWindow(targetWindow, selectionPoint);
            if (targetWindow != IntPtr.Zero)
            {
                FocusTargetWindow(targetWindow, focusWindow);
                await Task.Delay(180);
            }

            SendCtrlC();
            await WaitForClipboardChangeAsync(sequenceBefore);
            if (NativeMethods.GetClipboardSequenceNumber() == sequenceBefore && focusWindow != IntPtr.Zero)
            {
                _logger.Warn("Ctrl+C did not change clipboard. Trying WM_COPY.");
                NativeMethods.SendMessage(focusWindow, NativeMethods.WM_COPY, IntPtr.Zero, IntPtr.Zero);
                await WaitForClipboardChangeAsync(sequenceBefore);
            }

            var sequenceAfter = NativeMethods.GetClipboardSequenceNumber();
            if (sequenceAfter == sequenceBefore)
            {
                _logger.Warn("Clipboard sequence did not change after Ctrl+C.");
                return TextCaptureResult.Fail("未检测到可翻译文本", "clipboard");
            }

            string? copied = null;
            await WpfApplication.Current.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    copied = WpfClipboard.ContainsText() ? WpfClipboard.GetText() : null;
                }
                catch (Exception ex)
                {
                    _logger.Warn($"Clipboard read failed after copy: {ex.Message}");
                }
            });

            var cleaned = TextCleaner.Clean(copied);
            if (!IsValid(cleaned))
            {
                return TextCaptureResult.Fail("未检测到可翻译文本", "clipboard");
            }

            _logger.Info($"Text captured by clipboard. length={cleaned.Length}");
            return TextCaptureResult.Ok(cleaned, "clipboard");
        }
        catch (Exception ex)
        {
            _logger.Error("Clipboard capture failed.", ex);
            return TextCaptureResult.Fail("当前软件不支持直接取词，可尝试复制文本后再操作", "clipboard", ex);
        }
        finally
        {
            if (_settings.Capture.RestoreClipboard)
            {
                await RestoreClipboardAsync(hadOldText, oldText);
            }
        }
    }

    private async Task WaitForClipboardChangeAsync(uint sequenceBefore)
    {
        var timeoutAt = DateTimeOffset.UtcNow.AddMilliseconds(Math.Max(500, _settings.Capture.CopyWaitMs));
        while (DateTimeOffset.UtcNow < timeoutAt)
        {
            await Task.Delay(30);
            if (NativeMethods.GetClipboardSequenceNumber() != sequenceBefore)
            {
                return;
            }
        }
    }

    private async Task RestoreClipboardAsync(bool hadOldText, string? oldText)
    {
        await WpfApplication.Current.Dispatcher.InvokeAsync(() =>
        {
            try
            {
                if (hadOldText && oldText != null)
                {
                    WpfClipboard.SetText(oldText);
                }
                else
                {
                    WpfClipboard.Clear();
                }
            }
            catch (Exception ex)
            {
                _logger.Warn($"Failed to restore clipboard: {ex.Message}");
            }
        });
    }

    private bool IsValid(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        if (text.Length < _settings.Capture.MinTextLength || text.Length > _settings.Capture.MaxTextLength)
        {
            return false;
        }

        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lines.Length > 1 && lines.All(line => line.Contains(@":\") || line.StartsWith(@"\\", StringComparison.Ordinal)))
        {
            return false;
        }

        return true;
    }

    private void SendCtrlC()
    {
        var inputs = new[]
        {
            KeyInput(NativeMethods.VK_CONTROL, 0),
            KeyInput(NativeMethods.VK_C, 0),
            KeyInput(NativeMethods.VK_C, NativeMethods.KEYEVENTF_KEYUP),
            KeyInput(NativeMethods.VK_CONTROL, NativeMethods.KEYEVENTF_KEYUP)
        };

        var sent = NativeMethods.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<NativeMethods.INPUT>());
        if (sent != inputs.Length)
        {
            _logger.Warn($"SendInput Ctrl+C failed. sent={sent}, win32={Marshal.GetLastWin32Error()}, inputSize={Marshal.SizeOf<NativeMethods.INPUT>()}");
        }
    }

    private static IntPtr GetFocusWindow(IntPtr targetWindow, System.Windows.Point selectionPoint)
    {
        var point = new NativeMethods.POINT { X = (int)selectionPoint.X, Y = (int)selectionPoint.Y };
        var pointWindow = NativeMethods.WindowFromPoint(point);
        return pointWindow != IntPtr.Zero ? pointWindow : targetWindow;
    }

    private void FocusTargetWindow(IntPtr targetWindow, IntPtr focusWindow)
    {
        try
        {
            var currentThread = NativeMethods.GetCurrentThreadId();
            var targetThread = NativeMethods.GetWindowThreadProcessId(focusWindow != IntPtr.Zero ? focusWindow : targetWindow, out _);
            var attached = targetThread != 0 && targetThread != currentThread &&
                           NativeMethods.AttachThreadInput(currentThread, targetThread, true);
            try
            {
                NativeMethods.BringWindowToTop(targetWindow);
                NativeMethods.SetForegroundWindow(targetWindow);
                NativeMethods.SetFocus(focusWindow != IntPtr.Zero ? focusWindow : targetWindow);
            }
            finally
            {
                if (attached)
                {
                    NativeMethods.AttachThreadInput(currentThread, targetThread, false);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Warn($"Failed to focus target window: {ex.Message}");
        }
    }

    private static NativeMethods.INPUT KeyInput(ushort key, uint flags) => new()
    {
        type = NativeMethods.INPUT_KEYBOARD,
        U = new NativeMethods.INPUTUNION
        {
            ki = new NativeMethods.KEYBDINPUT
            {
                wVk = key,
                dwFlags = flags
            }
        }
    };
}
