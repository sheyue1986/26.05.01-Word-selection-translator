using System.Windows.Automation;
using DesktopAiTranslator.Models;
using DesktopAiTranslator.Utils;

namespace DesktopAiTranslator.Services;

public sealed class UIAutomationTextCaptureService : ITextCaptureService
{
    private readonly LoggingService _logger;

    public UIAutomationTextCaptureService(LoggingService logger)
    {
        _logger = logger;
    }

    public Task<TextCaptureResult> CaptureSelectedTextAsync(IntPtr targetWindow, System.Windows.Point selectionPoint)
    {
        try
        {
            AutomationElement? element = null;

            if (targetWindow != IntPtr.Zero)
            {
                try
                {
                    element = AutomationElement.FromHandle(targetWindow);
                }
                catch
                {
                    // Fall through to point/focus lookup.
                }
            }

            if (element == null)
            {
                try
                {
                    element = AutomationElement.FromPoint(selectionPoint);
                }
                catch
                {
                    // Fall through to focused element.
                }
            }

            element ??= AutomationElement.FocusedElement;
            if (element == null)
            {
                return Task.FromResult(TextCaptureResult.Fail("UI Automation 未找到元素", "uia"));
            }

            var text = TryGetSelectionText(element);
            if (string.IsNullOrWhiteSpace(text))
            {
                var focused = AutomationElement.FocusedElement;
                if (focused != null && !Equals(focused, element))
                {
                    text = TryGetSelectionText(focused);
                }
            }

            text = TextCleaner.Clean(text);
            if (string.IsNullOrWhiteSpace(text))
            {
                return Task.FromResult(TextCaptureResult.Fail("UI Automation 未检测到选区文本", "uia"));
            }

            _logger.Info($"Text captured by UI Automation. length={text.Length}");
            return Task.FromResult(TextCaptureResult.Ok(text, "uia"));
        }
        catch (Exception ex)
        {
            _logger.Warn($"UI Automation capture failed: {ex.Message}");
            return Task.FromResult(TextCaptureResult.Fail("UI Automation 取词失败", "uia", ex));
        }
    }

    private static string TryGetSelectionText(AutomationElement element)
    {
        if (!element.TryGetCurrentPattern(TextPattern.Pattern, out var patternObject) || patternObject is not TextPattern pattern)
        {
            return "";
        }

        var ranges = pattern.GetSelection();
        return string.Join(Environment.NewLine, ranges.Select(range => range.GetText(-1)));
    }
}
