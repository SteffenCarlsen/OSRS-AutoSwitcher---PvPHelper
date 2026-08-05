using PvPHelper.Core.Models;

namespace PvPHelper.Core.Automation;

/// <summary>Pure movement helpers — testable, no side effects. Mirrors the legacy LinearSmoothMove.</summary>
public static class Humanizer
{
    /// <summary>Randomise a target within +/- radius so repeated clicks don't land on identical pixels.</summary>
    public static ScreenPoint Jitter(ScreenPoint p, int radius, Random rng)
    {
        if (radius <= 0) return p;
        return new ScreenPoint(p.X + rng.Next(-radius, radius + 1), p.Y + rng.Next(-radius, radius + 1));
    }

    /// <summary>
    /// Interpolate <paramref name="steps"/> points from start toward end (exclusive of start).
    /// When <paramref name="curved"/> is set, follow a slight quadratic Bézier arc instead of a
    /// straight line (control point offset perpendicular to the path by a small random amount).
    /// </summary>
    public static IReadOnlyList<ScreenPoint> Path(ScreenPoint start, ScreenPoint end, int steps, bool curved = false, Random? rng = null)
    {
        steps = Math.Max(1, steps);
        var points = new List<ScreenPoint>(steps);

        if (curved && steps > 2)
        {
            rng ??= new Random();
            double dx = end.X - start.X, dy = end.Y - start.Y;
            double len = Math.Sqrt(dx * dx + dy * dy);
            double offset = len * 0.12 * (rng.NextDouble() * 2 - 1); // up to +/-12% of the distance
            double nx = len > 0 ? -dy / len : 0;
            double ny = len > 0 ? dx / len : 0;
            double cx = (start.X + end.X) / 2.0 + nx * offset;
            double cy = (start.Y + end.Y) / 2.0 + ny * offset;

            for (var i = 1; i <= steps; i++)
            {
                double t = (double)i / steps, u = 1 - t;
                double x = u * u * start.X + 2 * u * t * cx + t * t * end.X;
                double y = u * u * start.Y + 2 * u * t * cy + t * t * end.Y;
                points.Add(new ScreenPoint((int)Math.Round(x), (int)Math.Round(y)));
            }

            return points;
        }

        double sx = (end.X - start.X) / (double)steps;
        double sy = (end.Y - start.Y) / (double)steps;
        double px = start.X, py = start.Y;
        for (var i = 0; i < steps; i++)
        {
            px += sx;
            py += sy;
            points.Add(new ScreenPoint((int)Math.Round(px), (int)Math.Round(py)));
        }

        return points;
    }
}
