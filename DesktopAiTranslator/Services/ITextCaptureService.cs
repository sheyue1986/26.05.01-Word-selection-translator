using DesktopAiTranslator.Models;

namespace DesktopAiTranslator.Services;

public interface ITextCaptureService
{
    Task<TextCaptureResult> CaptureSelectedTextAsync(IntPtr targetWindow, System.Windows.Point selectionPoint);
}
