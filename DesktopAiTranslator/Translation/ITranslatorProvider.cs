namespace DesktopAiTranslator.Translation;

public interface ITranslatorProvider
{
    string Name { get; }

    Task<TranslationResult> TranslateAsync(TranslationRequest request, CancellationToken cancellationToken);
}
