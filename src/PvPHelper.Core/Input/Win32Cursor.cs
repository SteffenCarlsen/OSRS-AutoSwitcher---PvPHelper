using System.Runtime.InteropServices;
using PvPHelper.Core.Models;

namespace PvPHelper.Core.Input;

/// <summary>Reads the physical mouse cursor position (physical pixels). Used by calibration capture.</summary>
public static class Win32Cursor
{
    public static ScreenPoint GetPosition()
    {
        GetCursorPos(out var p);
        return new ScreenPoint(p.X, p.Y);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);
}
