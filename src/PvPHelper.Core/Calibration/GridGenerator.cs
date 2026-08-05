using PvPHelper.Core.Models;

namespace PvPHelper.Core.Calibration;

/// <summary>
/// Rebuilds inventory / prayer-book coordinate grids from a single anchor point plus spacing.
/// Storing anchor+spacing (rather than baked points) means re-anchoring and scale changes just work.
/// </summary>
public static class GridGenerator
{
    public const int InventoryColumns = 4;
    public const int InventoryRows = 7;
    public const int InventorySlots = InventoryColumns * InventoryRows; // 28

    /// <summary>Row-major list of 28 inventory slot points from the top-left anchor.</summary>
    public static IReadOnlyList<ScreenPoint> Inventory(ScreenPoint anchor, int spacingX, int spacingY)
    {
        var slots = new List<ScreenPoint>(InventorySlots);
        for (var row = 0; row < InventoryRows; row++)
        for (var col = 0; col < InventoryColumns; col++)
            slots.Add(new ScreenPoint(anchor.X + spacingX * col, anchor.Y + spacingY * row));
        return slots;
    }

    /// <summary>Maps each of the 29 prayers to a screen point from the first-prayer anchor.</summary>
    public static IReadOnlyDictionary<Prayer, ScreenPoint> PrayerBook(ScreenPoint anchor, int spacingX, int spacingY)
    {
        var map = new Dictionary<Prayer, ScreenPoint>(PrayerCatalog.Count);
        for (var i = 0; i < PrayerCatalog.Count; i++)
        {
            var col = i % PrayerCatalog.Columns;
            var row = i / PrayerCatalog.Columns;
            map[(Prayer)i] = new ScreenPoint(anchor.X + spacingX * col, anchor.Y + spacingY * row);
        }

        return map;
    }
}
