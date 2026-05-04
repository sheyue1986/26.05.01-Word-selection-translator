namespace DesktopAiTranslator.Translation;

public sealed class TranslationRequest
{
    public string Text { get; init; } = "";
    public string SourceLanguage { get; init; } = "auto";
    public string TargetLanguage { get; init; } = "zh-CN";
    public string Mode { get; init; } = "accurate";
    public string Provider { get; init; } = "Mock";
    public string Model { get; init; } = "";
    public double Temperature { get; init; } = 0.2;
    public int MaxTokens { get; init; } = 2000;
}
