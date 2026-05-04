namespace DesktopAiTranslator.Models;

public sealed class TextCaptureResult
{
    public bool Success { get; init; }
    public string Text { get; init; } = "";
    public string ErrorMessage { get; init; } = "";
    public string Method { get; init; } = "";
    public Exception? Exception { get; init; }

    public static TextCaptureResult Ok(string text, string method) => new()
    {
        Success = true,
        Text = text,
        Method = method
    };

    public static TextCaptureResult Fail(string message, string method = "", Exception? exception = null) => new()
    {
        Success = false,
        ErrorMessage = message,
        Method = method,
        Exception = exception
    };
}
