using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using DesktopAiTranslator.Utils;

namespace DesktopAiTranslator.Views;

public partial class FloatingTranslateButton : Window
{
    private readonly DispatcherTimer _timer;

    public event EventHandler? TranslateRequested;

    public FloatingTranslateButton(int autoHideMs)
    {
        InitializeComponent();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(Math.Max(300, autoHideMs)) };
        _timer.Tick += (_, _) => Close();
        Loaded += (_, _) => _timer.Start();
        MouseEnter += (_, _) => _timer.Stop();
        MouseLeave += (_, _) => _timer.Start();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var hwnd = new WindowInteropHelper(this).Handle;
        var style = NativeMethods.GetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE);
        NativeMethods.SetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE, new IntPtr(style.ToInt64() | NativeMethods.WS_EX_NOACTIVATE));
    }

    private void TranslateButton_Click(object sender, RoutedEventArgs e)
    {
        _timer.Stop();
        TranslateRequested?.Invoke(this, EventArgs.Empty);
    }
}
