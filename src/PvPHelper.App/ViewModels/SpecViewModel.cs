using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PvPHelper.App.Services;
using PvPHelper.Core.Models;

namespace PvPHelper.App.ViewModels;

public sealed partial class SpecViewModel : PageViewModelBase
{
    private readonly SessionState _session;

    public override string Title => "Spec Helper";
    public override string Subtitle => "Equip a spec weapon, activate, click the target";
    public override string Glyph => "☄";

    public ObservableCollection<SpecItemVm> Specs { get; } = new();

    [ObservableProperty] private SpecItemVm? _selected;

    public bool HasSelection => Selected is not null;

    public SpecViewModel(SessionState session)
    {
        _session = session;
        foreach (var model in session.Profile.Specs)
            Specs.Add(new SpecItemVm(model, _session.Save));
        Selected = Specs.FirstOrDefault();
    }

    partial void OnSelectedChanged(SpecItemVm? value) => OnPropertyChanged(nameof(HasSelection));

    [RelayCommand]
    private void Add()
    {
        var model = new SpecBinding { Name = "New spec", SpecTimes = 1 };
        _session.Profile.Specs.Add(model);
        var vm = new SpecItemVm(model, _session.Save);
        Specs.Add(vm);
        Selected = vm;
        _session.Save();
    }

    [RelayCommand]
    private void Delete(SpecItemVm? item)
    {
        if (item is null) return;
        _session.Profile.Specs.Remove(item.Model);
        Specs.Remove(item);
        if (ReferenceEquals(Selected, item)) Selected = Specs.FirstOrDefault();
        _session.Save();
    }
}

public sealed partial class SpecItemVm : ObservableObject
{
    private readonly Action _persist;
    private readonly OrderedSlotSelection _selection = new();

    public SpecBinding Model { get; }
    public ObservableCollection<SlotCellVm> Cells { get; } = new();

    [ObservableProperty] private string _name;
    [ObservableProperty] private HotkeyToken _hotkey;
    [ObservableProperty] private bool _waitForSpecBar;
    [ObservableProperty] private int _specTimes;

    public SpecItemVm(SpecBinding model, Action persist)
    {
        Model = model;
        _persist = persist;
        _name = model.Name;
        _hotkey = model.Hotkey;
        _waitForSpecBar = model.WaitForSpecBar;
        _specTimes = Math.Max(1, model.SpecTimes);
        _selection.Set(model.WeaponSlots);
        for (var i = 0; i < 28; i++) Cells.Add(new SlotCellVm(i, ToggleSlot));
        RefreshCells();
    }

    public string DisplayName => string.IsNullOrWhiteSpace(Name) ? "(unnamed)" : Name;
    public string HotkeyText => Hotkey.Display;
    public string Summary => $"{_selection.Count} slot(s) · {SpecTimes}× spec";

    partial void OnNameChanged(string value) { Model.Name = value; OnPropertyChanged(nameof(DisplayName)); _persist(); }
    partial void OnHotkeyChanged(HotkeyToken value) { Model.Hotkey = value; OnPropertyChanged(nameof(HotkeyText)); _persist(); }
    partial void OnWaitForSpecBarChanged(bool value) { Model.WaitForSpecBar = value; _persist(); }

    partial void OnSpecTimesChanged(int value)
    {
        Model.SpecTimes = value;
        OnPropertyChanged(nameof(Summary));
        _persist();
    }

    [RelayCommand] private void IncrementTimes() => SpecTimes++;
    [RelayCommand] private void DecrementTimes() => SpecTimes = Math.Max(1, SpecTimes - 1);

    private void ToggleSlot(SlotCellVm cell)
    {
        _selection.Toggle(cell.Index);
        Model.WeaponSlots = _selection.ToList();
        RefreshCells();
        OnPropertyChanged(nameof(Summary));
        _persist();
    }

    private void RefreshCells()
    {
        foreach (var c in Cells)
        {
            c.IsSelected = _selection.Contains(c.Index);
            c.Ordinal = _selection.OrdinalOf(c.Index);
        }
    }
}
