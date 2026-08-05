using PvPHelper.Core.Calibration;
using PvPHelper.Core.Models;

namespace PvPHelper.Tests;

public class GridGeneratorTests
{
    [Fact]
    public void Inventory_Generates_28_Slots_RowMajor()
    {
        var slots = GridGenerator.Inventory(new ScreenPoint(100, 200), 40, 35);

        Assert.Equal(28, slots.Count);
        Assert.Equal(new ScreenPoint(100, 200), slots[0]);   // slot 1  (col0,row0)
        Assert.Equal(new ScreenPoint(220, 200), slots[3]);   // slot 4  (col3,row0)
        Assert.Equal(new ScreenPoint(100, 235), slots[4]);   // slot 5  (col0,row1)
        Assert.Equal(new ScreenPoint(220, 410), slots[27]);  // slot 28 (col3,row6)
    }

    [Fact]
    public void PrayerBook_Generates_29_Prayers()
    {
        var book = GridGenerator.PrayerBook(new ScreenPoint(0, 0), 40, 40);

        Assert.Equal(29, book.Count);
        Assert.Equal(new ScreenPoint(0, 0), book[Prayer.ThickSkin]);     // index 0
        Assert.Equal(new ScreenPoint(160, 0), book[Prayer.MysticWill]);  // index 4  (col4,row0)
        Assert.Equal(new ScreenPoint(0, 40), book[Prayer.RockSkin]);     // index 5  (col0,row1)
        Assert.Equal(new ScreenPoint(80, 200), book[Prayer.Rigour]);     // index 27 (col2,row5)
        Assert.Equal(new ScreenPoint(120, 200), book[Prayer.Augury]);    // index 28 (col3,row5)
    }
}
