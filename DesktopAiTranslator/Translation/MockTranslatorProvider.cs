namespace DesktopAiTranslator.Translation;

public sealed class MockTranslatorProvider : ITranslatorProvider
{
    public string Name => "Mock";

    public Task<TranslationResult> TranslateAsync(TranslationRequest request, CancellationToken cancellationToken)
    {
        var result = TranslationResult.Ok($"【模拟翻译】{Environment.NewLine}{request.Text}", Name, "mock");
        return Task.FromResult(result);
    }
}
