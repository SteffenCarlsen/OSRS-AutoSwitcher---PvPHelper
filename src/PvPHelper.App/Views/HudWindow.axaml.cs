using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using PvPHelper.App.Services;

namespace PvPHelper.App.Views;

/// <summary>
/// Small always-on-top status readout, shown while armed. Draggable, and its position is
/// remembered per-profile — so after the first move it never sits over the inventory again.
/// The default (first-run) spot is the top-centre of the screen, clear of the OSRS side panels.
/// </summary>
public partial class HudWindow : Window
{
    private const int DefaultWidth = 230;

    public HudWindow() => InitializeComponent();

    private SessionState? Session => DataContext as SessionState;

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        // Restore the saved position if we have one.
        if (Session?.Profile is { HudX: { } savedX, HudY: { } savedY })
        {
            Position = new PixelPoint(savedX, savedY);
            return;
        }

        // First run: top-centre of the primary work area (away from the bottom-right inventory).
        var screen = Screens.Primary ?? Screens.All.FirstOrDefault();
        if (screen is not null)
        {
            var area = screen.WorkingArea;
            var width = Bounds.Width > 0 ? (int)Bounds.Width : DefaultWidth;
            Position = new PixelPoint(area.X + (area.Width - width) / 2, area.Y + 12);
        }
    }

    private void OnDrag(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;

        BeginMoveDrag(e); // runs the move loop; returns once the drag ends
        SavePosition();
    }

    private void SavePosition()
    {
        if (Session is not { Profile: { } profile } session) return;

        profile.HudX = Position.X;
        profile.HudY = Position.Y;
        session.Save();
    }
}
