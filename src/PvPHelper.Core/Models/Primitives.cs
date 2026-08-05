namespace PvPHelper.Core.Models;

/// <summary>An absolute screen pixel coordinate (multi-monitor aware).</summary>
public readonly record struct ScreenPoint(int X, int Y)
{
    public override string ToString() => $"({X}, {Y})";
}

/// <summary>Where a hotkey comes from — keyboard or a mouse button (Mouse 4 / 5 supported).</summary>
public enum HotkeySource
{
    Keyboard,
    Mouse
}

/// <summary>
/// A single bindable hotkey. For <see cref="HotkeySource.Keyboard"/> the code is a Win32
/// virtual-key code; for <see cref="HotkeySource.Mouse"/> it is a button number (1=L,2=R,3=M,4,5).
/// </summary>
public readonly record struct HotkeyToken(HotkeySource Source, int Code)
{
    public static HotkeyToken None => new(HotkeySource.Keyboard, 0);

    public bool IsBound => !(Source == HotkeySource.Keyboard && Code == 0);

    public string Display => HotkeyNames.Describe(this);

    public override string ToString() => Display;
}

/// <summary>Human-readable names for hotkeys, for the UI key-chips.</summary>
public static class HotkeyNames
{
    public static string Describe(HotkeyToken token)
    {
        if (!token.IsBound) return "—";

        // Mouse codes are Win32 virtual-key codes (as delivered by the low-level hook):
        // 1=L, 2=R, 4=Middle, 5=XButton1 (Mouse 4), 6=XButton2 (Mouse 5).
        if (token.Source == HotkeySource.Mouse)
            return token.Code switch
            {
                1 => "Mouse L",
                2 => "Mouse R",
                4 => "Mouse M",
                5 => "Mouse 4",
                6 => "Mouse 5",
                _ => $"Mouse {token.Code}"
            };

        return DescribeVirtualKey(token.Code);
    }

    private static string DescribeVirtualKey(int vk)
    {
        if (vk is >= 0x30 and <= 0x39) return ((char)vk).ToString();          // 0-9
        if (vk is >= 0x41 and <= 0x5A) return ((char)vk).ToString();          // A-Z
        if (vk is >= 0x70 and <= 0x87) return "F" + (vk - 0x6F);              // F1-F24
        if (vk is >= 0x60 and <= 0x69) return "Num " + (vk - 0x60);           // Numpad 0-9

        return vk switch
        {
            0x1B => "Esc",
            0x20 => "Space",
            0x09 => "Tab",
            0x0D => "Enter",
            0x08 => "Backspace",
            0x10 => "Shift",
            0x11 => "Ctrl",
            0x12 => "Alt",
            0x2E => "Del",
            0x2D => "Ins",
            0x24 => "Home",
            0x23 => "End",
            0x21 => "PgUp",
            0x22 => "PgDn",
            0x25 => "Left",
            0x26 => "Up",
            0x27 => "Right",
            0x28 => "Down",
            0xC0 => "`",
            _ => "VK " + vk
        };
    }
}
