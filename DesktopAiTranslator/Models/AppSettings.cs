namespace DesktopAiTranslator.Models;

public sealed class AppSettings
{
    public SelectionButtonSettings SelectionButton { get; set; } = new();
    public CaptureSettings Capture { get; set; } = new();
    public TranslationSettings Translation { get; set; } = new();
    public ProviderSettings Providers { get; set; } = new();
    public List<string> ExcludedProcesses { get; set; } = new() { "Photoshop.exe", "AutoCAD.exe" };
}

public sealed class SelectionButtonSettings
{
    public bool Enabled { get; set; } = true;
    public int MinDragDistancePx { get; set; } = 8;
    public int MinDragDurationMs { get; set; } = 120;
    public int MaxDragDurationMs { get; set; } = 8000;
    public int ButtonAutoHideMs { get; set; } = 2000;
    public int ButtonOffsetX { get; set; } = 10;
    public int ButtonOffsetY { get; set; } = 10;
    public bool AllowFullscreen { get; set; }
}

public sealed class CaptureSettings
{
    public List<string> MethodOrder { get; set; } = new() { "uia", "clipboard" };
    public int CopyWaitMs { get; set; } = 180;
    public bool RestoreClipboard { get; set; } = true;
    public int MinTextLength { get; set; } = 1;
    public int MaxTextLength { get; set; } = 5000;
}

public sealed class TranslationSettings
{
    public string Provider { get; set; } = "Mock";
    public string TargetLanguage { get; set; } = "zh-CN";
    public string Mode { get; set; } = "accurate";
    public double Temperature { get; set; } = 0.2;
    public int TimeoutSeconds { get; set; } = 30;
}

public sealed class ProviderSettings
{
    public OpenAiCompatibleProviderSettings Qwen { get; set; } = new()
    {
        BaseUrl = "https://dashscope.aliyuncs.com/compatible-mode/v1",
        Model = "qwen-plus"
    };

    public OpenAiCompatibleProviderSettings DeepSeek { get; set; } = new()
    {
        BaseUrl = "https://api.deepseek.com",
        Model = "deepseek-chat"
    };
}

public sealed class OpenAiCompatibleProviderSettings
{
    public string BaseUrl { get; set; } = "";
    public string Model { get; set; } = "";
    public string ApiKeyProtected { get; set; } = "";
}
