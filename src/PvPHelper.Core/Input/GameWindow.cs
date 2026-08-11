using System.Runtime.InteropServices;
using System.Text;
using PvPHelper.Core.Models;

namespace PvPHelper.Core.Input;

/// <summary>Supplies the HWND of the game window for background (PostMessage) input.</summary>
public interface IGameWindowProvider
{
    /// <summary>The target window handle, or 0 if none is currently found.</summary>
    nint TargetWindow { get; }
}

/// <summary>
/// Locates a top-level game window by title substring (RuneLite / OSRS by default),
/// or uses an explicitly selected handle. Also converts screen points to client points.
/// </summary>
public sealed class GameWindowLocator : IGameWindowProvider
{
    private static readonly string[] DefaultTitles = { "RuneLite", "Old School RuneScape", "RuneScape" };

    /// <summary>When non-zero, this handle is used directly (user-picked window).</summary>
    public nint ExplicitWindow { get; set; }

    /// <summary>Title substrings to match, most-specific first.</summary>
    public IReadOnlyList<string> TitleCandidates { get; set; } = DefaultTitles;

    public void UseTitle(string? title) =>
        TitleCandidates = string.IsNullOrWhiteSpace(title) ? DefaultTitles : new[] { title! };

    public nint TargetWindow =>
        ExplicitWindow != 0 && Win32Windows.IsWindow(ExplicitWindow)
            ? ExplicitWindow
            : Win32Windows.FindTopLevel(TitleCandidates);
}

/// <summary>A selectable top-level window (handle + visible title).</summary>
public readonly record struct WindowInfo(nint Handle, string Title)
{
    public override string ToString() => Title;
}

/// <summary>Win32 window helpers (enumeration + coordinate conversion).</summary>
public static class Win32Windows
{
    public static bool TryScreenToClient(nint hwnd, ScreenPoint p, out int clientX, out int clientY)
    {
        var pt = new POINT { X = p.X, Y = p.Y };
        if (ScreenToClient(hwnd, ref pt))
        {
            clientX = pt.X;
            clientY = pt.Y;
            return true;
        }

        clientX = 0;
        clientY = 0;
        return false;
    }

    /// <summary>First visible top-level window whose title contains any candidate (case-insensitive).</summary>
    public static nint FindTopLevel(IReadOnlyList<string> titleSubstrings)
    {
        nint found = 0;
        var sb = new StringBuilder(512);

        EnumWindows((hwnd, _) =>
        {
            if (!IsWindowVisible(hwnd)) return true;
            var len = GetWindowText(hwnd, sb, sb.Capacity);
            if (len == 0) return true;
            var title = sb.ToString(0, len);

            foreach (var candidate in titleSubstrings)
            {
                if (title.Contains(candidate, StringComparison.OrdinalIgnoreCase))
                {
                    found = hwnd;
                    return false; // stop enumerating
                }
            }

            return true;
        }, 0);

        return found;
    }

    /// <summary>
    /// Every visible, titled, top-level window a user could pick as the input target.
    /// Tool windows and DWM-cloaked ghost windows (e.g. background UWP apps) are filtered out
    /// so the list stays close to the "real" windows on screen.
    /// </summary>
    public static IReadOnlyList<WindowInfo> EnumerateTopLevel()
    {
        var list = new List<WindowInfo>();
        var sb = new StringBuilder(512);

        EnumWindows((hwnd, _) =>
        {
            if (!IsWindowVisible(hwnd)) return true;

            var exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            if ((exStyle & WS_EX_TOOLWINDOW) != 0) return true; // skip tool windows (tooltips, etc.)

            if (IsCloaked(hwnd)) return true; // skip invisible UWP/DWM ghosts

            var len = GetWindowText(hwnd, sb, sb.Capacity);
            if (len == 0) return true;

            list.Add(new WindowInfo(hwnd, sb.ToString(0, len)));
            return true;
        }, 0);

        return list;
    }

    private static bool IsCloaked(nint hwnd)
    {
        try
        {
            return DwmGetWindowAttribute(hwnd, DWMWA_CLOAKED, out var cloaked, sizeof(int)) == 0 && cloaked != 0;
        }
        catch
        {
            return false; // dwmapi unavailable — treat as not cloaked
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X; public int Y; }

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int DWMWA_CLOAKED = 14;

    private delegate bool EnumWindowsProc(nint hwnd, nint lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, nint lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(nint hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(nint hWnd);

    [DllImport("user32.dll")]
    public static extern bool IsWindow(nint hWnd);

    [DllImport("user32.dll")]
    private static extern bool ScreenToClient(nint hWnd, ref POINT lpPoint);

    // GWL_EXSTYLE fits in 32 bits, so GetWindowLongW is correct on both x86 and x64.
    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
    private static extern int GetWindowLong(nint hWnd, int nIndex);

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(nint hwnd, int attr, out int value, int size);
}
