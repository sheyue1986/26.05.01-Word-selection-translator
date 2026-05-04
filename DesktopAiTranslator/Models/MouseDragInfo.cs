using WpfPoint = System.Windows.Point;

namespace DesktopAiTranslator.Models;

public sealed class MouseDragInfo
{
    public WpfPoint StartPoint { get; init; }
    public WpfPoint EndPoint { get; init; }
    public double DragDistance { get; init; }
    public TimeSpan DragDuration { get; init; }
    public IntPtr StartForegroundWindow { get; init; }
    public IntPtr EndForegroundWindow { get; init; }
}
