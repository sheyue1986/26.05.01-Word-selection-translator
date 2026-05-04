using System.Text.RegularExpressions;

namespace DesktopAiTranslator.Utils;

public static class TextCleaner
{
    public static string Clean(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "";
        }

        var value = text.Replace("\r\n", "\n").Replace('\r', '\n').Trim();
        value = Regex.Replace(value, @"([A-Za-z])-\n([A-Za-z])", "$1$2");
        value = Regex.Replace(value, @"(?<![。！？；：.!?:;\n])\n(?!\n|\s*[-*•]|\s*\d+[\.\)]|\s*\d+\.\d)", " ");
        value = Regex.Replace(value, @"[ \t]+\n", "\n");
        value = Regex.Replace(value, @"\n[ \t]+", "\n");
        value = Regex.Replace(value, @"\n{3,}", "\n\n");
        return value.Trim();
    }
}
