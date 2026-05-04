using System.Drawing;
using System.IO;
using DesktopAiTranslator.Models;
using DesktopAiTranslator.Views;
using Forms = System.Windows.Forms;
using WpfApplication = System.Windows.Application;

namespace DesktopAiTranslator.Services;

public sealed class TrayService : IDisposable
{
    private readonly AppSettings _settings;
    private readonly SettingsService _settingsService;
    private readonly SelectionButtonService _selectionButtonService;
    private readonly LoggingService _logger;
    private Forms.NotifyIcon? _notifyIcon;
    private Forms.ToolStripMenuItem? _toggleItem;
    private SettingsWindow? _settingsWindow;

    public TrayService(
        AppSettings settings,
        SettingsService settingsService,
        MouseHookService mouseHookService,
        SelectionButtonService selectionButtonService,
        LoggingService logger)
    {
        _settings = settings;
        _settingsService = settingsService;
        _selectionButtonService = selectionButtonService;
        _logger = logger;
    }

    public void Initialize()
    {
        _toggleItem = new Forms.ToolStripMenuItem(GetToggleText(), null, (_, _) => ToggleEnabled());
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add(_toggleItem);
        menu.Items.Add(new Forms.ToolStripMenuItem("设置", null, (_, _) => ShowSettings()));
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(new Forms.ToolStripMenuItem("退出", null, (_, _) => Exit()));

        _notifyIcon = new Forms.NotifyIcon
        {
            Icon = LoadTrayIcon(),
            Text = "AI 划词翻译器",
            Visible = true,
            ContextMenuStrip = menu
        };
        _notifyIcon.DoubleClick += (_, _) => ShowSettings();
        _notifyIcon.ShowBalloonTip(1800, "AI 划词翻译器", "已在系统托盘运行，划选文字后会出现“译”按钮。", Forms.ToolTipIcon.Info);
        _logger.Info("Tray initialized.");
    }

    private static Icon LoadTrayIcon()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico");
        return File.Exists(path) ? new Icon(path) : SystemIcons.Application;
    }

    private string GetToggleText() => _settings.SelectionButton.Enabled ? "暂停划词翻译" : "启用划词翻译";

    private void ToggleEnabled()
    {
        _settings.SelectionButton.Enabled = !_settings.SelectionButton.Enabled;
        if (_toggleItem != null)
        {
            _toggleItem.Text = GetToggleText();
        }

        if (!_settings.SelectionButton.Enabled)
        {
            _selectionButtonService.HideButton();
        }

        _settingsService.Save();
        _logger.Info($"Selection translation enabled={_settings.SelectionButton.Enabled}");
    }

    private void ShowSettings()
    {
        if (_settingsWindow != null)
        {
            _settingsWindow.Activate();
            return;
        }

        _settingsWindow = new SettingsWindow(_settings, _settingsService);
        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow.Show();
    }

    private void Exit()
    {
        _logger.Info("Exit requested from tray.");
        _settingsService.Save();
        WpfApplication.Current.Shutdown();
    }

    public void Dispose()
    {
        _notifyIcon?.Dispose();
        _notifyIcon = null;
    }
}
