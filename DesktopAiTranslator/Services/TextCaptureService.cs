using DesktopAiTranslator.Models;

namespace DesktopAiTranslator.Services;

public sealed class TextCaptureService : ITextCaptureService
{
    private readonly UIAutomationTextCaptureService _uia;
    private readonly ClipboardTextCaptureService _clipboard;
    private readonly LoggingService _logger;

    public TextCaptureService(UIAutomationTextCaptureService uia, ClipboardTextCaptureService clipboard, LoggingService logger)
    {
        _uia = uia;
        _clipboard = clipboard;
        _logger = logger;
    }

    public async Task<TextCaptureResult> CaptureSelectedTextAsync(IntPtr targetWindow, System.Windows.Point selectionPoint)
    {
        var uia = await _uia.CaptureSelectedTextAsync(targetWindow, selectionPoint);
        if (uia.Success)
        {
            return uia;
        }

        _logger.Info("UI Automation returned no text. Falling back to clipboard.");
        var clipboard = await _clipboard.CaptureSelectedTextAsync(targetWindow, selectionPoint);
        return clipboard.Success ? clipboard : TextCaptureResult.Fail(clipboard.ErrorMessage, clipboard.Method, clipboard.Exception);
    }
}
