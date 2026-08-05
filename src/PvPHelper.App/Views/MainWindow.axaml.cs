using Avalonia.Controls;
using Avalonia.Input;

namespace PvPHelper.App.Views;

public partial class MainWindow : Window
{
    private bool _allowClose;

    public MainWindow() => InitializeComponent();

    /// <summary>Permit the window to actually close (used by the tray "Quit").</summary>
    public void AllowClose() => _allowClose = true;

    // Closing the window hides it to the tray instead of quitting.
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (!_allowClose)
        {
            e.Cancel = true;
            Hide();
        }

        base.OnClosing(e);
    }

    // Allow dragging the window from the branding header.
    private void OnTitleBarPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            BeginMoveDrag(e);
    }
}
