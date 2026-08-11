using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using PvPHelper.App.Services;
using PvPHelper.Core.Models;

namespace PvPHelper.App.ViewModels;

public sealed partial class HotkeysViewModel : PageViewModelBase
{
    private readonly SessionState _session;

    public override string Title => "Hotkeys";
    public override string Subtitle => "Every binding in one place · conflicts flagged";
    public override string Glyph => "⌨";

    public ObservableCollection<HotkeyRowVm> Bindings { get; } = new();
    public ObservableCollection<HotkeyRowVm> GameTabs { get; } = new();
    public ObservableCollection<HotkeyRowVm> AppKeys { get; } = new();

    public bool HasBindings => Bindings.Count > 0;

    public HotkeysViewModel(SessionState session)
    {
        _session = session;
        Rebuild();
    }

    public override void OnNavigatedTo() => Rebuild();

    private void Rebuild()
    {
        Bindings.Clear();
        GameTabs.Clear();
        AppKeys.Clear();
        var p = _session.Profile;

        foreach (var s in p.Switches)
            Bindings.Add(new HotkeyRowVm("⚔", "Auto-switch", s.Name, () => s.Hotkey, v => s.Hotkey = v, OnChanged));
        foreach (var pr in p.Prayers)
            Bindings.Add(new HotkeyRowVm("✦", "Prayer", PrayerCatalog.DisplayName(pr.Prayer), () => pr.Hotkey, v => pr.Hotkey = v, OnChanged));
        foreach (var sp in p.Specs)
            Bindings.Add(new HotkeyRowVm("☄", "Spec", sp.Name, () => sp.Hotkey, v => sp.Hotkey = v, OnChanged));

        // In-game tab F-keys the engine presses to open a panel before clicking it.
        var t = p.Tabs;
        GameTabs.Add(new HotkeyRowVm("▦", "Game tab", "Inventory tab", () => t.Inventory, v => t.Inventory = v, OnChanged));
        GameTabs.Add(new HotkeyRowVm("✦", "Game tab", "Prayer tab", () => t.Prayer, v => t.Prayer = v, OnChanged));
        GameTabs.Add(new HotkeyRowVm("⚔", "Game tab", "Combat / attack-style tab", () => t.Attack, v => t.Attack = v, OnChanged));

        AppKeys.Add(new HotkeyRowVm("△", "Application", "Master arm / disarm", () => p.ArmHotkey, v => p.ArmHotkey = v, OnChanged));
        AppKeys.Add(new HotkeyRowVm("⛔", "Application", "Panic / stop", () => p.PanicKey, v => p.PanicKey = v, OnChanged));

        DetectConflicts();
        OnPropertyChanged(nameof(HasBindings));
    }

    private void OnChanged()
    {
        DetectConflicts();
        _session.Save();
    }

    private void DetectConflicts()
    {
        var counts = Bindings.Concat(AppKeys)
            .Where(r => r.Hotkey.IsBound)
            .GroupBy(r => r.Hotkey)
            .ToDictionary(g => g.Key, g => g.Count());

        foreach (var row in Bindings.Concat(AppKeys))
            row.HasConflict = row.Hotkey.IsBound && counts.TryGetValue(row.Hotkey, out var c) && c > 1;
    }
}

/// <summary>A row in the Hotkeys table, editing one binding's hotkey through get/set delegates.</summary>
public sealed partial class HotkeyRowVm : ObservableObject
{
    private readonly Action<HotkeyToken> _set;
    private readonly Action _changed;

    public string Glyph { get; }
    public string Kind { get; }
    public string Name { get; }

    [ObservableProperty] private HotkeyToken _hotkey;
    [ObservableProperty] private bool _hasConflict;

    public HotkeyRowVm(string glyph, string kind, string name,
        Func<HotkeyToken> get, Action<HotkeyToken> set, Action changed)
    {
        Glyph = glyph;
        Kind = kind;
        Name = name;
        _set = set;
        _changed = changed;
        _hotkey = get();
    }

    partial void OnHotkeyChanged(HotkeyToken value)
    {
        _set(value);
        _changed();
    }
}
