using DesktopAiTranslator.Models;
using DesktopAiTranslator.Translation;

namespace DesktopAiTranslator.Services;

public sealed class TranslatorService
{
    private readonly AppSettings _settings;
    private readonly Dictionary<string, ITranslatorProvider> _providers;
    private readonly LoggingService _logger;

    public TranslatorService(AppSettings settings, IEnumerable<ITranslatorProvider> providers, LoggingService logger)
    {
        _settings = settings;
        _providers = providers.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
        _logger = logger;
    }

    public async Task<TranslationResult> TranslateAsync(string text, CancellationToken cancellationToken)
    {
        var providerName = _settings.Translation.Provider;
        if (!_providers.TryGetValue(providerName, out var provider))
        {
            provider = _providers["Mock"];
        }

        _logger.Info($"Translate requested. provider={provider.Name}, target={_settings.Translation.TargetLanguage}, textLength={text.Length}");
        var request = new TranslationRequest
        {
            Text = text,
            TargetLanguage = _settings.Translation.TargetLanguage,
            Mode = _settings.Translation.Mode,
            Provider = provider.Name,
            Temperature = _settings.Translation.Temperature
        };

        try
        {
            return await provider.TranslateAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Error("Translator provider failed.", ex);
            return TranslationResult.Fail("翻译失败，请稍后重试", provider.Name);
        }
    }
}
