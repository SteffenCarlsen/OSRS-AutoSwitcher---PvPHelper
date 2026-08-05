using PvPHelper.Core.Models;

namespace PvPHelper.Tests;

public class OrderedSlotSelectionTests
{
    [Fact]
    public void Toggle_Adds_In_Click_Order()
    {
        var s = new OrderedSlotSelection();
        s.Toggle(5);
        s.Toggle(2);
        s.Toggle(9);

        Assert.Equal(new[] { 5, 2, 9 }, s.Order);
        Assert.Equal(1, s.OrdinalOf(5));
        Assert.Equal(2, s.OrdinalOf(2));
        Assert.Equal(3, s.OrdinalOf(9));
    }

    [Fact]
    public void Toggle_Removes_And_Renumbers()
    {
        var s = new OrderedSlotSelection();
        s.Set(new[] { 5, 2, 9 });

        s.Toggle(2); // remove the middle one

        Assert.Equal(new[] { 5, 9 }, s.Order);
        Assert.False(s.Contains(2));
        Assert.Equal(0, s.OrdinalOf(2));
        Assert.Equal(2, s.OrdinalOf(9)); // 9 renumbered from 3rd to 2nd
    }

    [Fact]
    public void Set_Deduplicates_Preserving_Order()
    {
        var s = new OrderedSlotSelection();
        s.Set(new[] { 5, 2, 5, 9, 2 });

        Assert.Equal(new[] { 5, 2, 9 }, s.Order);
    }
}
