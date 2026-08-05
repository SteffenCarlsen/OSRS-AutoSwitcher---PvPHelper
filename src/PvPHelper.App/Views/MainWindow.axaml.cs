using Avalonia.Controls;
using Avalonia.Input;

namespace PvPHelper.App.Views;

public partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();

    // Allow dragging the window from the branding header.
    private void OnTitleBarPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            BeginMoveDrag(e);
    }
}
