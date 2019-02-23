using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace OSRSAutoSwitcher.Globals
{
    public static class WindowsImports
    {
        [DllImport("user32.dll")]
        public static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        public static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

        public const int MOUSEEVENTF_LEFTDOWN = 0x02;
        public const int MOUSEEVENTF_LEFTUP = 0x04;
        public static void LeftMouseClick(int xpos, int ypos)
        {
            mouse_event(MOUSEEVENTF_LEFTDOWN, xpos, ypos, 0, 0);
            mouse_event(MOUSEEVENTF_LEFTUP, xpos, ypos, 0, 0);
        }
        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(Keys vKey);

    }
}
