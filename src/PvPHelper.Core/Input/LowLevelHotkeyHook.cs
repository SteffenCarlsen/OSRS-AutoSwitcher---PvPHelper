using LowLevelInput.Hooks;
using PvPHelper.Core.Models;

namespace PvPHelper.Core.Input;

/// <summary>
/// Global low-level keyboard + mouse hook (supports Mouse 4/5) built on LowLevelInput.Net.
/// Raises <see cref="HotkeyReleased"/> on key/button up. The hook is non-blocking and
/// ignores our own injected input (clearInjectedFlag) so synthetic clicks don't feed back.
/// </summary>
public sealed class LowLevelHotkeyHook : IHotkeyHook
{
    private readonly object _gate = new();
    private InputManager? _manager;

    public event Action<HotkeyEvent>? HotkeyReleased;

    public void Start()
    {
        lock (_gate)
        {
            if (_manager != null) return;
            _manager = new InputManager();
            // captureMouseMove: false, clearInjectedFlag: true, installMouseHook: true
            _manager.Initialize(false, true, true);
            _manager.OnKeyboardEvent += OnKeyboard;
            _manager.OnMouseEvent += OnMouse;
        }
    }

    public void Stop()
    {
        lock (_gate)
        {
            if (_manager == null) return;
            _manager.OnKeyboardEvent -= OnKeyboard;
            _manager.OnMouseEvent -= OnMouse;
            _manager.Dispose();
            _manager = null;
        }
    }

    private void OnKeyboard(VirtualKeyCode key, KeyState state)
    {
        if (state == KeyState.Up)
            HotkeyReleased?.Invoke(new HotkeyEvent(new HotkeyToken(HotkeySource.Keyboard, (int)key)));
    }

    private void OnMouse(VirtualKeyCode key, KeyState state, int x, int y)
    {
        if (state != KeyState.Up) return;
        switch (key)
        {
            case VirtualKeyCode.Lbutton:
            case VirtualKeyCode.Rbutton:
            case VirtualKeyCode.Mbutton:
            case VirtualKeyCode.Xbutton1:
            case VirtualKeyCode.Xbutton2:
                HotkeyReleased?.Invoke(new HotkeyEvent(new HotkeyToken(HotkeySource.Mouse, (int)key)));
                break;
        }
    }

    public void Dispose() => Stop();
}
