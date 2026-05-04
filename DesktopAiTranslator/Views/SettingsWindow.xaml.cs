using System.Windows;
using System.Windows.Controls;
using DesktopAiTranslator.Models;
using DesktopAiTranslator.Services;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace DesktopAiTranslator.Views;

public partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;
    private readonly SettingsService _settingsService;
    private readonly CredentialService _credentials = new();

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

    public SettingsWindow(AppSettings settings, SettingsService settingsService)
    {
        InitializeComponent();
        _settings = settings;
        _settingsService = settingsService;
        LoadLanguageOptions();
        LoadValues();
    }

    private void LoadLanguageOptions()
    {
        foreach (var language in Languages)
        {
            TargetLanguageComboBox.Items.Add(new ComboBoxItem
            {
                Content = language.Name,
                Tag = language.Code
            });
        }
    }

    private void LoadValues()
    {
        EnabledCheckBox.IsChecked = _settings.SelectionButton.Enabled;
        AutoHideTextBox.Text = _settings.SelectionButton.ButtonAutoHideMs.ToString();
        MinDistanceTextBox.Text = _settings.SelectionButton.MinDragDistancePx.ToString();
        MinDurationTextBox.Text = _settings.SelectionButton.MinDragDurationMs.ToString();
        MaxDurationTextBox.Text = _settings.SelectionButton.MaxDragDurationMs.ToString();
        CopyWaitTextBox.Text = _settings.Capture.CopyWaitMs.ToString();
        MaxTextLengthTextBox.Text = _settings.Capture.MaxTextLength.ToString();
        RestoreClipboardCheckBox.IsChecked = _settings.Capture.RestoreClipboard;
        TemperatureTextBox.Text = _settings.Translation.Temperature.ToString("0.##");

        SelectComboByContent(ProviderComboBox, _settings.Translation.Provider);
        SelectComboByContent(ModeComboBox, _settings.Translation.Mode);
        SelectLanguage(_settings.Translation.TargetLanguage);

        QwenBaseUrlTextBox.Text = _settings.Providers.Qwen.BaseUrl;
        QwenModelTextBox.Text = _settings.Providers.Qwen.Model;
        QwenApiKeyBox.Password = SafeUnprotect(_settings.Providers.Qwen.ApiKeyProtected);

        DeepSeekBaseUrlTextBox.Text = _settings.Providers.DeepSeek.BaseUrl;
        DeepSeekModelTextBox.Text = _settings.Providers.DeepSeek.Model;
        DeepSeekApiKeyBox.Password = SafeUnprotect(_settings.Providers.DeepSeek.ApiKeyProtected);
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        _settings.SelectionButton.Enabled = EnabledCheckBox.IsChecked == true;
        _settings.SelectionButton.ButtonAutoHideMs = ReadInt(AutoHideTextBox, _settings.SelectionButton.ButtonAutoHideMs);
        _settings.SelectionButton.MinDragDistancePx = ReadInt(MinDistanceTextBox, _settings.SelectionButton.MinDragDistancePx);
        _settings.SelectionButton.MinDragDurationMs = ReadInt(MinDurationTextBox, _settings.SelectionButton.MinDragDurationMs);
        _settings.SelectionButton.MaxDragDurationMs = ReadInt(MaxDurationTextBox, _settings.SelectionButton.MaxDragDurationMs);
        _settings.Capture.CopyWaitMs = ReadInt(CopyWaitTextBox, _settings.Capture.CopyWaitMs);
        _settings.Capture.MaxTextLength = ReadInt(MaxTextLengthTextBox, _settings.Capture.MaxTextLength);
        _settings.Capture.RestoreClipboard = RestoreClipboardCheckBox.IsChecked == true;

        _settings.Translation.Provider = SelectedContent(ProviderComboBox, "Mock");
        _settings.Translation.Mode = SelectedContent(ModeComboBox, "accurate");
        _settings.Translation.TargetLanguage = SelectedLanguage();
        _settings.Translation.Temperature = ReadDouble(TemperatureTextBox, _settings.Translation.Temperature);

        _settings.Providers.Qwen.BaseUrl = NonEmpty(QwenBaseUrlTextBox.Text, "https://dashscope.aliyuncs.com/compatible-mode/v1");
        _settings.Providers.Qwen.Model = NonEmpty(QwenModelTextBox.Text, "qwen-plus");
        _settings.Providers.Qwen.ApiKeyProtected = Protect(QwenApiKeyBox.Password);

        _settings.Providers.DeepSeek.BaseUrl = NonEmpty(DeepSeekBaseUrlTextBox.Text, "https://api.deepseek.com");
        _settings.Providers.DeepSeek.Model = NonEmpty(DeepSeekModelTextBox.Text, "deepseek-chat");
        _settings.Providers.DeepSeek.ApiKeyProtected = Protect(DeepSeekApiKeyBox.Password);

        _settingsService.Save();
        Close();
    }

    private void SelectLanguage(string code)
    {
        foreach (ComboBoxItem item in TargetLanguageComboBox.Items)
        {
            if (string.Equals(item.Tag as string, code, StringComparison.OrdinalIgnoreCase))
            {
                TargetLanguageComboBox.SelectedItem = item;
                return;
            }
        }

        TargetLanguageComboBox.SelectedIndex = 0;
    }

    private string SelectedLanguage()
    {
        return TargetLanguageComboBox.SelectedItem is ComboBoxItem item && item.Tag is string code ? code : "zh-CN";
    }

    private static void SelectComboByContent(WpfComboBox comboBox, string value)
    {
        foreach (ComboBoxItem item in comboBox.Items)
        {
            if (string.Equals(item.Content?.ToString(), value, StringComparison.OrdinalIgnoreCase))
            {
                comboBox.SelectedItem = item;
                return;
            }
        }

        comboBox.SelectedIndex = 0;
    }

    private static string SelectedContent(WpfComboBox comboBox, string fallback)
    {
        return comboBox.SelectedItem is ComboBoxItem item ? item.Content?.ToString() ?? fallback : fallback;
    }

    private static int ReadInt(WpfTextBox textBox, int fallback)
    {
        return int.TryParse(textBox.Text, out var value) && value > 0 ? value : fallback;
    }

    private static double ReadDouble(WpfTextBox textBox, double fallback)
    {
        return double.TryParse(textBox.Text, out var value) && value >= 0 ? value : fallback;
    }

    private static string NonEmpty(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private string Protect(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "" : _credentials.Protect(value.Trim());
    }

    private string SafeUnprotect(string value)
    {
        try
        {
            return _credentials.Unprotect(value);
        }
        catch
        {
            return "";
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
