using WpfClipboard = System.Windows.Clipboard;

namespace DesktopAiTranslator.Utils;

public static class ClipboardHelper
{
    public static string? GetTextSafe()
    {
        try
        {
            return WpfClipboard.ContainsText() ? WpfClipboard.GetText() : null;
        }
        catch
        {
            return null;
        }
    }

    public static void SetTextSafe(string? text)
    {
        try
        {
            if (text == null)
            {
                WpfClipboard.Clear();
            }
            else
            {
                WpfClipboard.SetText(text);
            }
        }
        catch
        {
        }
    }
}
