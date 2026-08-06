using PostMessageInput;
using PvPHelper.Core.Models;

namespace PvPHelper.Core.Input;

/// <summary>
/// Delivers input to the game via posted window messages (no window focus required).
/// Calibrated screen coordinates are converted to the target window's client area.
/// <see cref="GetCursor"/> reports the real cursor (there is no synthetic cursor), so the
/// engine's "return to previous position" moves post the mouse back to where it actually is.
/// Failures (window gone/invalid) are traced and swallowed so a stray action can't crash the app.
/// </summary>
public sealed class PostMessageInputSender : IInputSender
{
    private readonly IGameWindowProvider _windows;

    public PostMessageInputSender(IGameWindowProvider windows) => _windows = windows;

    public ScreenPoint GetCursor() => Win32Cursor.GetPosition();

    public void SetCursor(ScreenPoint point) =>
        WithTarget((input, hwnd) =>
        {
            if (Win32Windows.TryScreenToClient(hwnd, point, out var x, out var y))
                input.MoveMouse(x, y);
        });

    public void LeftClick(ScreenPoint point) =>
        WithTarget((input, hwnd) =>
        {
            if (Win32Windows.TryScreenToClient(hwnd, point, out var x, out var y))
                input.Click(MouseButton.Left, x, y);
        });

    public void PressKey(HotkeyToken key)
    {
        if (key.Source != HotkeySource.Keyboard || !key.IsBound) return;
        WithTarget((input, _) => input.PressKey((ushort)key.Code));
    }

    private void WithTarget(Action<ProcessInput, nint> action)
    {
        var hwnd = _windows.TargetWindow;
        if (hwnd == 0) return;

        try
        {
            action(ProcessInput.FromWindow(hwnd), hwnd);
        }
        catch (Exception ex)
        {
            // Window changed/closed or the UI thread has no focus for keyboard posting; don't let
            // one failed post tear down an action sequence, but leave a trace for diagnosis.
            System.Diagnostics.Debug.WriteLine($"[PostMessageInputSender] post to 0x{hwnd:X} failed: {ex.Message}");
        }
    }
}
