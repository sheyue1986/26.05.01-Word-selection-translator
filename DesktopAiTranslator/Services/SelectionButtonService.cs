using System.Windows.Interop;
using DesktopAiTranslator.Models;
using DesktopAiTranslator.Utils;
using DesktopAiTranslator.Views;
using WpfPoint = System.Windows.Point;
using WpfWindow = System.Windows.Window;

namespace DesktopAiTranslator.Services;

public sealed class SelectionButtonService : IDisposable
{
    private readonly AppSettings _settings;
    private readonly ITextCaptureService _textCapture;
    private readonly TranslatorService _translator;
    private readonly LoggingService _logger;
    private FloatingTranslateButton? _button;
    private TranslationPopup? _popup;
    private string _currentText = "";
    private IntPtr _targetWindow;
    private WpfPoint _selectionPoint;
    private TextCaptureResult? _prefetchedText;

    public SelectionButtonService(AppSettings settings, ITextCaptureService textCapture, TranslatorService translator, LoggingService logger)
    {
        _settings = settings;
        _textCapture = textCapture;
        _translator = translator;
        _logger = logger;
    }

    public async Task ShowButtonAsync(WpfPoint releasePoint, IntPtr targetWindow)
    {
        HideButton();
        _targetWindow = targetWindow;
        _selectionPoint = releasePoint;
        _currentText = "";
        _prefetchedText = await PrefetchSelectedTextAsync(_targetWindow, _selectionPoint);

        var location = new WpfPoint(
            releasePoint.X + _settings.SelectionButton.ButtonOffsetX,
            releasePoint.Y + _settings.SelectionButton.ButtonOffsetY);

        _button = new FloatingTranslateButton(_settings.SelectionButton.ButtonAutoHideMs);
        _button.TranslateRequested += async (_, _) => await TranslateAtAsync(releasePoint);
        _button.Closed += (_, _) => _button = null;
        var onScreen = ScreenHelper.KeepWindowOnScreen(location, 32, 32);
        _button.Left = onScreen.X;
        _button.Top = onScreen.Y;
        _button.Show();
        _logger.Info("Floating translate button shown.");
    }

    public void HideButton()
    {
        if (_button == null)
        {
            return;
        }

        var button = _button;
        _button = null;
        button.Close();
    }

    public bool IsPointerOverOwnWindow(WpfPoint point)
    {
        return IsPointInside(_button, point) || IsPointInside(_popup, point);
    }

    private static bool IsPointInside(WpfWindow? window, WpfPoint point)
    {
        if (window == null || !window.IsVisible)
        {
            return false;
        }

        var helper = new WindowInteropHelper(window);
        if (helper.Handle == IntPtr.Zero || !NativeMethods.GetWindowRect(helper.Handle, out var rect))
        {
            return false;
        }

        return point.X >= rect.Left && point.X <= rect.Right && point.Y >= rect.Top && point.Y <= rect.Bottom;
    }

    private async Task<TextCaptureResult> PrefetchSelectedTextAsync(IntPtr targetWindow, WpfPoint selectionPoint)
    {
        try
        {
            var result = await _textCapture.CaptureSelectedTextAsync(targetWindow, selectionPoint);
            if (result.Success)
            {
                _logger.Info($"Prefetched selected text. method={result.Method}, length={result.Text.Length}");
            }
            else
            {
                _logger.Warn($"Prefetch failed. method={result.Method}, error={result.ErrorMessage}");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.Error("Prefetch selected text failed.", ex);
            return TextCaptureResult.Fail("未检测到可翻译文本", "prefetch", ex);
        }
    }

    private async Task TranslateAtAsync(WpfPoint releasePoint)
    {
        HideButton();

        try
        {
            var capture = _prefetchedText ?? TextCaptureResult.Fail("未检测到可翻译文本", "prefetch");
            if (!capture.Success)
            {
                _logger.Warn("Prefetch had no text. Trying live capture once.");
                capture = await _textCapture.CaptureSelectedTextAsync(_targetWindow, _selectionPoint);
            }

            _popup?.Close();
            ShowPopup(releasePoint);

            if (!capture.Success)
            {
                _popup?.ShowError("未检测到可翻译文本");
                _logger.Warn($"Capture failed. method={capture.Method}, error={capture.ErrorMessage}");
                return;
            }

            if (capture.Text.Length > _settings.Capture.MaxTextLength)
            {
                _popup?.ShowError("翻译文本过长");
                return;
            }

            _currentText = capture.Text;
            await TranslateCurrentTextAsync();
        }
        catch (Exception ex)
        {
            _logger.Error("Translate flow failed.", ex);
            _popup?.Close();
            ShowPopup(releasePoint);
            _popup?.ShowError("翻译失败，请稍后重试");
        }
    }

    private async Task TranslateCurrentTextAsync()
    {
        if (_popup == null || string.IsNullOrWhiteSpace(_currentText))
        {
            return;
        }

        _popup.ShowLoading();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_settings.Translation.TimeoutSeconds));
        var result = await _translator.TranslateAsync(_currentText, cts.Token);
        if (result.Success)
        {
            _popup.ShowTranslation(result.Translation);
            _logger.Info($"Translation succeeded. provider={result.Provider}, translationLength={result.Translation.Length}");
        }
        else
        {
            _popup.ShowError(result.ErrorMessage);
            _logger.Warn($"Translation failed. provider={result.Provider}, error={result.ErrorMessage}");
        }
    }

    private void ShowPopup(WpfPoint releasePoint)
    {
        if (_popup != null)
        {
            return;
        }

        _popup = new TranslationPopup();
        _popup.SetTargetLanguage(_settings.Translation.TargetLanguage);
        _popup.TargetLanguageChanged += async (_, languageCode) =>
        {
            _settings.Translation.TargetLanguage = languageCode;
            await TranslateCurrentTextAsync();
        };
        _popup.RetryRequested += async (_, _) => await TranslateCurrentTextAsync();
        _popup.Closed += (_, _) => _popup = null;
        var popupPoint = ScreenHelper.KeepWindowOnScreen(
            new WpfPoint(releasePoint.X + 16, releasePoint.Y + 16),
            390,
            320);
        _popup.Left = popupPoint.X;
        _popup.Top = popupPoint.Y;
        _popup.ShowLoading();
        _popup.Show();
    }

    public void Dispose()
    {
        HideButton();
        _popup?.Close();
        _popup = null;
    }
}
