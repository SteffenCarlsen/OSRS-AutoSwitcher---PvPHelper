using PvPHelper.Core.Models;

namespace PvPHelper.Core.Input;

/// <summary>Sends synthetic input to the game window. Implemented over PostMessage.</summary>
public interface IInputSender
{
    ScreenPoint GetCursor();
    void SetCursor(ScreenPoint point);
    void LeftClick(ScreenPoint point);
    void PressKey(HotkeyToken key);
}

/// <summary>Raised when a bound key/mouse button is released.</summary>
public readonly record struct HotkeyEvent(HotkeyToken Token);

/// <summary>Global low-level keyboard + mouse hook (supports Mouse 4/5). Wraps LowLevelInput.Net.</summary>
public interface IHotkeyHook : IDisposable
{
    event Action<HotkeyEvent>? HotkeyReleased;
    void Start();
    void Stop();
}
