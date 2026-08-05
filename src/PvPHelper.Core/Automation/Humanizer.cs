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

    /// <summary>Interpolate <paramref name="steps"/> points from start toward end (exclusive of start).</summary>
    public static IReadOnlyList<ScreenPoint> Path(ScreenPoint start, ScreenPoint end, int steps)
    {
        steps = Math.Max(1, steps);
        var points = new List<ScreenPoint>(steps);
        double dx = (end.X - start.X) / (double)steps;
        double dy = (end.Y - start.Y) / (double)steps;
        double x = start.X;
        double y = start.Y;
        for (var i = 0; i < steps; i++)
        {
            x += dx;
            y += dy;
            points.Add(new ScreenPoint((int)Math.Round(x), (int)Math.Round(y)));
        }

        return points;
    }
}
