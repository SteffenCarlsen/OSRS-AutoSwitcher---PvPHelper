using System.Runtime.InteropServices;
using PvPHelper.Core.Models;

namespace PvPHelper.Core.Input;

/// <summary>Sends synthetic mouse/keyboard input to the focused game window via Win32 SendInput.</summary>
public sealed class Win32InputSender : IInputSender
{
    public ScreenPoint GetCursor()
    {
        GetCursorPos(out var p);
        return new ScreenPoint(p.X, p.Y);
    }

    public void SetCursor(ScreenPoint point) => SetCursorPos(point.X, point.Y);

    public void LeftClick(ScreenPoint point)
    {
        SetCursorPos(point.X, point.Y);
        var inputs = new[] { NewMouse(MOUSEEVENTF_LEFTDOWN), NewMouse(MOUSEEVENTF_LEFTUP) };
        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    public void PressKey(HotkeyToken key)
    {
        if (key.Source != HotkeySource.Keyboard || !key.IsBound) return;
        var inputs = new[] { NewKey((ushort)key.Code, false), NewKey((ushort)key.Code, true) };
        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    // ---------- interop ----------
    private const uint INPUT_MOUSE = 0;
    private const uint INPUT_KEYBOARD = 1;
    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    private static INPUT NewMouse(uint flags) => new()
    {
        type = INPUT_MOUSE,
        U = new InputUnion { mi = new MOUSEINPUT { dwFlags = flags } }
    };

    private static INPUT NewKey(ushort vk, bool keyUp) => new()
    {
        type = INPUT_KEYBOARD,
        U = new InputUnion { ki = new KEYBDINPUT { wVk = vk, dwFlags = keyUp ? KEYEVENTF_KEYUP : 0 } }
    };

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X; public int Y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT { public uint type; public InputUnion U; }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public nint dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public nint dwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);
}
