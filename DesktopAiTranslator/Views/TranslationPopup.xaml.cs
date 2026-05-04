using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using DesktopAiTranslator.Utils;
using WpfClipboard = System.Windows.Clipboard;

namespace DesktopAiTranslator.Views;

public partial class TranslationPopup : Window
{
    private bool _isLoadingLanguages;
    private int _caseMode;
    private string _originalTranslation = "";

    public event EventHandler<string>? TargetLanguageChanged;
    public event EventHandler? RetryRequested;

    private static readonly (string Code, string Name)[] Languages =
    {
        ("zh-CN", "中文（简体）"),
        ("zh-TW", "中文（繁体）"),
        ("en", "English"),
        ("ja", "日本語"),
        ("ko", "한국어"),
        ("fr", "Français"),
        ("de", "Deutsch"),
        ("es", "Español"),
        ("pt", "Português"),
        ("it", "Italiano"),
        ("ru", "Русский"),
        ("ar", "العربية"),
        ("hi", "हिन्दी"),
        ("vi", "Tiếng Việt"),
        ("th", "ไทย"),
        ("id", "Bahasa Indonesia")
    };

    public TranslationPopup()
    {
        InitializeComponent();
        LoadLanguages();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var hwnd = new WindowInteropHelper(this).Handle;
        var style = NativeMethods.GetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE);
        NativeMethods.SetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE, new IntPtr(style.ToInt64()));
    }

    public void SetTargetLanguage(string code)
    {
        _isLoadingLanguages = true;
        foreach (ComboBoxItem item in TargetLanguageComboBox.Items)
        {
            if (string.Equals(item.Tag as string, code, StringComparison.OrdinalIgnoreCase))
            {
                TargetLanguageComboBox.SelectedItem = item;
                _isLoadingLanguages = false;
                return;
            }
        }

        TargetLanguageComboBox.SelectedIndex = 0;
        _isLoadingLanguages = false;
    }

    public void ShowLoading()
    {
        _caseMode = 0;
        _originalTranslation = "";
        CaseButton.Content = "Aa";
        TranslationTextBox.Text = "";
        StatusText.Text = "翻译中...";
        RetryButton.Visibility = Visibility.Collapsed;
    }

    public void ShowTranslation(string translation)
    {
        _caseMode = 0;
        _originalTranslation = translation;
        CaseButton.Content = "Aa";
        TranslationTextBox.Text = translation;
        StatusText.Text = "";
        RetryButton.Visibility = Visibility.Collapsed;
    }

    public void ShowError(string message)
    {
        _caseMode = 0;
        _originalTranslation = "";
        CaseButton.Content = "Aa";
        TranslationTextBox.Text = "";
        StatusText.Text = message;
        RetryButton.Visibility = Visibility.Visible;
    }

    private void LoadLanguages()
    {
        _isLoadingLanguages = true;
        foreach (var language in Languages)
        {
            TargetLanguageComboBox.Items.Add(new ComboBoxItem
            {
                Content = language.Name,
                Tag = language.Code
            });
        }

        TargetLanguageComboBox.SelectedIndex = 0;
        _isLoadingLanguages = false;
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void TargetLanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoadingLanguages || TargetLanguageComboBox.SelectedItem is not ComboBoxItem item)
        {
            return;
        }

        if (item.Tag is string code)
        {
            TargetLanguageChanged?.Invoke(this, code);
        }
    }

    private void CaseButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_originalTranslation))
        {
            return;
        }

        _caseMode = (_caseMode % 3) + 1;
        TranslationTextBox.Text = _caseMode switch
        {
            1 => _originalTranslation.ToUpper(CultureInfo.CurrentCulture),
            2 => _originalTranslation.ToLower(CultureInfo.CurrentCulture),
            _ => ToTitleCase(_originalTranslation)
        };
        CaseButton.Content = _caseMode switch
        {
            1 => "AA",
            2 => "aa",
            _ => "Aa"
        };
    }

    private static string ToTitleCase(string text)
    {
        var lower = text.ToLower(CultureInfo.CurrentCulture);
        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(lower);
    }

    private void CopyTranslation_Click(object sender, RoutedEventArgs e)
    {
        WpfClipboard.SetText(TranslationTextBox.Text ?? "");
        FlashButton(CopyTranslationButton, "已复制", "复制译文");
    }

    private void Retry_Click(object sender, RoutedEventArgs e)
    {
        RetryRequested?.Invoke(this, EventArgs.Empty);
    }

    private static void FlashButton(System.Windows.Controls.Button button, string temporaryText, string originalText)
    {
        button.Content = temporaryText;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(900) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            button.Content = originalText;
        };
        timer.Start();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
