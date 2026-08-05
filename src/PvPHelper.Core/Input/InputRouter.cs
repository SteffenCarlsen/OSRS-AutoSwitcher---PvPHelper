using PvPHelper.Core.Models;

namespace PvPHelper.Core.Input;

/// <summary>Reports the currently selected input mode (implemented by the app from the active profile).</summary>
public interface IInputModeProvider
{
    InputMode Mode { get; }
}

/// <summary>
/// An <see cref="IInputSender"/> that dispatches each call to the foreground (SendInput)
/// or background (PostMessage) sender based on the current <see cref="InputMode"/>.
/// The engine is unaware of which is active.
/// </summary>
public sealed class InputRouter : IInputSender
{
    private readonly IInputModeProvider _mode;
    private readonly IInputSender _foreground;
    private readonly IInputSender _background;

    public InputRouter(IInputModeProvider mode, IInputSender foreground, IInputSender background)
    {
        _mode = mode;
        _foreground = foreground;
        _background = background;
    }

    private IInputSender Active => _mode.Mode == InputMode.Background ? _background : _foreground;

    public ScreenPoint GetCursor() => Active.GetCursor();
    public void SetCursor(ScreenPoint point) => Active.SetCursor(point);
    public void LeftClick(ScreenPoint point) => Active.LeftClick(point);
    public void PressKey(HotkeyToken key) => Active.PressKey(key);
}
