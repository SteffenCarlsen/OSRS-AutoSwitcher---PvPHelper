using PostMessageInput;
using PvPHelper.Core.Models;

namespace PvPHelper.Core.Input;

/// <summary>
/// Background input via posted window messages. Screen coordinates (from calibration) are
/// converted to the target window's client area. There is no real cursor, so a virtual
/// cursor is tracked for the engine's "return to previous position" moves.
/// Failures (window gone/invalid) are swallowed so a stray action can't crash the app.
/// </summary>
public sealed class PostMessageInputSender : IInputSender
{
    private readonly IGameWindowProvider _windows;
    private ScreenPoint _cursor;

    public PostMessageInputSender(IGameWindowProvider windows) => _windows = windows;

    public ScreenPoint GetCursor() => _cursor;

    public void SetCursor(ScreenPoint point)
    {
        _cursor = point;
        WithTarget((input, hwnd) =>
        {
            if (Win32Windows.TryScreenToClient(hwnd, point, out var x, out var y))
                input.MoveMouse(x, y);
        });
    }

    public void LeftClick(ScreenPoint point)
    {
        _cursor = point;
        WithTarget((input, hwnd) =>
        {
            if (Win32Windows.TryScreenToClient(hwnd, point, out var x, out var y))
                input.Click(MouseButton.Left, x, y);
        });
    }

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
        catch
        {
            // Window changed/closed or the UI thread has no focus for keyboard posting;
            // ignore so one failed post can't tear down an action sequence.
        }
    }
}
