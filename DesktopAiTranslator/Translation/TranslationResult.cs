namespace DesktopAiTranslator.Translation;

public sealed class TranslationResult
{
    public bool Success { get; init; }
    public string Translation { get; init; } = "";
    public string Provider { get; init; } = "";
    public string Model { get; init; } = "";
    public string ErrorMessage { get; init; } = "";
    public int? InputTokens { get; init; }
    public int? OutputTokens { get; init; }

    public static TranslationResult Ok(string translation, string provider, string model = "") => new()
    {
        Success = true,
        Translation = translation,
        Provider = provider,
        Model = model
    };

    public static TranslationResult Fail(string message, string provider, string model = "") => new()
    {
        Success = false,
        ErrorMessage = message,
        Provider = provider,
        Model = model
    };
}
