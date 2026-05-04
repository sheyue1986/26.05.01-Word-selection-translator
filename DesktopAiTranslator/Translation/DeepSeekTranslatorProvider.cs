namespace DesktopAiTranslator.Translation;

public sealed class DeepSeekTranslatorProvider : QwenTranslatorProvider
{
    public override string Name => "DeepSeek";

    public DeepSeekTranslatorProvider(Func<string> baseUrlProvider, Func<string> modelProvider, Func<string> apiKeyProvider)
        : base(baseUrlProvider, modelProvider, apiKeyProvider)
    {
    }
}
