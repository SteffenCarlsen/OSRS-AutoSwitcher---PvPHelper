using Avalonia.Input;

namespace PvPHelper.App.Services;

/// <summary>Maps Avalonia keys to Win32 virtual-key codes so captured hotkeys match the hook.</summary>
public static class KeyMap
{
    public static int ToVirtualKey(Key key) => key switch
    {
        >= Key.A and <= Key.Z => 0x41 + (key - Key.A),
        >= Key.D0 and <= Key.D9 => 0x30 + (key - Key.D0),
        >= Key.NumPad0 and <= Key.NumPad9 => 0x60 + (key - Key.NumPad0),
        >= Key.F1 and <= Key.F12 => 0x70 + (key - Key.F1),
        Key.Space => 0x20,
        Key.Tab => 0x09,
        Key.Enter => 0x0D,
        Key.Back => 0x08,
        Key.Left => 0x25,
        Key.Up => 0x26,
        Key.Right => 0x27,
        Key.Down => 0x28,
        Key.OemTilde => 0xC0,
        Key.Delete => 0x2E,
        Key.Insert => 0x2D,
        Key.Home => 0x24,
        Key.End => 0x23,
        Key.PageUp => 0x21,
        Key.PageDown => 0x22,
        Key.LeftShift or Key.RightShift => 0x10,
        Key.LeftCtrl or Key.RightCtrl => 0x11,
        Key.LeftAlt or Key.RightAlt => 0x12,
        _ => 0
    };
}
