using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace AutoSwitcher
{
    public static class MouseEvents
    {
        [DllImport("user32.dll")]
        private static extern bool BlockInput(bool block);

        public static void FreezeMouse()
        {
            BlockInput(true);
        }

        public static void ThawMouse()
        {
            BlockInput(false);
        }
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool SetCursorPos(int x, int y);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

        public const int MOUSEEVENTF_LEFTDOWN = 0x02;
        public const int MOUSEEVENTF_LEFTUP = 0x04;

        //This simulates a left mouse click
        public static void LeftMouseClick(int xpos, int ypos)
        {
            mouse_event(MOUSEEVENTF_LEFTDOWN, xpos, ypos, 0, 0);
            mouse_event(MOUSEEVENTF_LEFTUP, xpos, ypos, 0, 0);
        }

        public static void LinearSmoothMove(Point newPosition, int steps,bool click)
        {
            Point start = Cursor.Position;
            PointF iterPoint = start;

            // Find the slope of the line segment defined by start and newPosition
            PointF slope = new PointF(newPosition.X - start.X, newPosition.Y - start.Y);

            // Divide by the number of steps
            slope.X = slope.X / steps;
            slope.Y = slope.Y / steps;

            // Move the mouse to each iterative point.
            for (int i = 0; i < steps; i++)
            {
                iterPoint = new PointF(iterPoint.X + slope.X, iterPoint.Y + slope.Y);
                Point roundedPoint = new Point(Convert.ToInt32(iterPoint.X), Convert.ToInt32(iterPoint.Y));
                SetCursorPos(roundedPoint.X, roundedPoint.Y);
                Thread.Sleep(10);
            }
            // Move the mouse to the final destination.
            SetCursorPos(newPosition.X, newPosition.Y);
            Thread.Sleep(50);
            if (click)
            {
                LeftMouseClick(newPosition.X, newPosition.Y);
            }
        }
    }
}