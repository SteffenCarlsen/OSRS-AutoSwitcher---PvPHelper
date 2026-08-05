namespace PvPHelper.Core.Models;

/// <summary>
/// An ordered, toggleable set of inventory slot indices. The order is the click order,
/// which is the execution order. Pure logic so the picker's behaviour is unit-testable.
/// </summary>
public sealed class OrderedSlotSelection
{
    private readonly List<int> _order = new();

    public IReadOnlyList<int> Order => _order;
    public int Count => _order.Count;

    public bool Contains(int slot) => _order.Contains(slot);

    /// <summary>1-based position in the click order, or 0 if not selected.</summary>
    public int OrdinalOf(int slot)
    {
        var i = _order.IndexOf(slot);
        return i < 0 ? 0 : i + 1;
    }

    public void Toggle(int slot)
    {
        var i = _order.IndexOf(slot);
        if (i >= 0) _order.RemoveAt(i);
        else _order.Add(slot);
    }

    public void Set(IEnumerable<int> slots)
    {
        _order.Clear();
        _order.AddRange(slots);
    }

    public void Clear() => _order.Clear();

    public List<int> ToList() => new(_order);
}
