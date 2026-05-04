using DesktopAiTranslator.Services;
using DesktopAiTranslator.Translation;
using WpfApplication = System.Windows.Application;
using StartupEventArgs = System.Windows.StartupEventArgs;
using ExitEventArgs = System.Windows.ExitEventArgs;

namespace DesktopAiTranslator;

public partial class App : WpfApplication
{
    private LoggingService? _logger;
    private SettingsService? _settingsService;
    private MouseHookService? _mouseHookService;
    private TrayService? _trayService;
    private SelectionButtonService? _selectionButtonService;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _logger = new LoggingService();
        _logger.Info("Application starting.");

        DispatcherUnhandledException += (_, args) =>
        {
            _logger.Error("Unhandled UI exception.", args.Exception);
            args.Handled = true;
        };

        _settingsService = new SettingsService(_logger);
        var settings = _settingsService.Load();
        var credentials = new CredentialService();

        var textCapture = new TextCaptureService(
            new UIAutomationTextCaptureService(_logger),
            new ClipboardTextCaptureService(settings, _logger),
            _logger);

        var translator = new TranslatorService(
            settings,
            new ITranslatorProvider[]
            {
                new MockTranslatorProvider(),
                new QwenTranslatorProvider(
                    () => settings.Providers.Qwen.BaseUrl,
                    () => settings.Providers.Qwen.Model,
                    () => SafeUnprotect(credentials, settings.Providers.Qwen.ApiKeyProtected)),
                new DeepSeekTranslatorProvider(
                    () => settings.Providers.DeepSeek.BaseUrl,
                    () => settings.Providers.DeepSeek.Model,
                    () => SafeUnprotect(credentials, settings.Providers.DeepSeek.ApiKeyProtected))
            },
            _logger);

        _selectionButtonService = new SelectionButtonService(settings, textCapture, translator, _logger);
        _mouseHookService = new MouseHookService(settings, _selectionButtonService, _logger);
        _trayService = new TrayService(settings, _settingsService, _mouseHookService, _selectionButtonService, _logger);

        _trayService.Initialize();
        _mouseHookService.Start();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _logger?.Info("Application exiting.");
        _mouseHookService?.Dispose();
        _selectionButtonService?.Dispose();
        _trayService?.Dispose();
        _settingsService?.Save();
        _logger?.Dispose();
        base.OnExit(e);
    }

    private static string SafeUnprotect(CredentialService credentials, string protectedValue)
    {
        try
        {
            return credentials.Unprotect(protectedValue);
        }
        catch
        {
            return "";
        }
    }
}
