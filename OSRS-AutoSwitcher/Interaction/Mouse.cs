using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using OSRSAutoSwitcher.Globals;

namespace OSRSAutoSwitcher.Interaction
{
    public class Mouse
    {
        public static void LinearSmoothMove(Point newPosition, int steps, bool click)
        {
            Point start = Cursor.Position;
            PointF iterPoint = start;

            //Randomize End position to not click at same coords multiple times
            newPosition = GetRandomizedPoint(newPosition);

            // Find the slope of the line segment defined by start and newPosition
            PointF slope = new PointF(newPosition.X - start.X, newPosition.Y - start.Y);

            // Divide by the number of steps(Smooth value)
            slope.X = slope.X / steps;
            slope.Y = slope.Y / steps;

            // Move the mouse to each iterative point.
            for (int i = 0; i < steps; i++)
            {
                iterPoint = new PointF(iterPoint.X + slope.X, iterPoint.Y + slope.Y);
                Point roundedPoint = new Point(Convert.ToInt32(iterPoint.X), Convert.ToInt32(iterPoint.Y));
                WindowsImports.SetCursorPos(roundedPoint.X, roundedPoint.Y);
                //Natural delay in mouse movement
                Thread.Sleep(10);
            }
            // Move the mouse to the final destination.
            WindowsImports.SetCursorPos(newPosition.X, newPosition.Y);

            //Click at last position?
            Thread.Sleep(50);
            if (click)
            {
                WindowsImports.LeftMouseClick(newPosition.X, newPosition.Y);
            }
        }



        private static Point GetRandomizedPoint(Point p)
        {
            Point point = new Point();
            Random r = new Random();

            point.X = r.Next(p.X - 5, p.X + 5);
            point.Y = r.Next(p.Y - 5, p.Y + 5);

            return point;

        }
    }
}
