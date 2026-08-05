using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PvPHelper.App.Services;
using PvPHelper.Core.Models;

namespace PvPHelper.App.ViewModels;

public sealed partial class AutoSwitchViewModel : PageViewModelBase
{
    private readonly SessionState _session;

    public override string Title => "Auto-Switch";
    public override string Subtitle => "Bind gear switches to a key";
    public override string Glyph => "⚔";

    public ObservableCollection<SwitchItemVm> Switches { get; } = new();

    [ObservableProperty]
    private SwitchItemVm? _selected;

    public bool HasSelection => Selected is not null;

    public AutoSwitchViewModel(SessionState session)
    {
        _session = session;
        foreach (var model in session.Profile.Switches)
            Switches.Add(new SwitchItemVm(model, _session.Save));
        Selected = Switches.FirstOrDefault();
    }

    partial void OnSelectedChanged(SwitchItemVm? value) => OnPropertyChanged(nameof(HasSelection));

    [RelayCommand]
    private void Add()
    {
        var model = new SwitchBinding { Name = "New switch" };
        _session.Profile.Switches.Add(model);
        var vm = new SwitchItemVm(model, _session.Save);
        Switches.Add(vm);
        Selected = vm;
        _session.Save();
    }

    [RelayCommand]
    private void Delete(SwitchItemVm? item)
    {
        if (item is null) return;
        _session.Profile.Switches.Remove(item.Model);
        Switches.Remove(item);
        if (ReferenceEquals(Selected, item)) Selected = Switches.FirstOrDefault();
        _session.Save();
    }
}

/// <summary>Editable wrapper over one <see cref="SwitchBinding"/>. Writes through to the model.</summary>
public sealed partial class SwitchItemVm : ObservableObject
{
    private readonly Action _persist;
    private readonly OrderedSlotSelection _selection = new();

    public SwitchBinding Model { get; }
    public ObservableCollection<SlotCellVm> Cells { get; } = new();

    [ObservableProperty] private string _name;
    [ObservableProperty] private HotkeyToken _hotkey;

    public SwitchItemVm(SwitchBinding model, Action persist)
    {
        Model = model;
        _persist = persist;
        _name = model.Name;
        _hotkey = model.Hotkey;
        _selection.Set(model.SlotOrder);
        for (var i = 0; i < 28; i++) Cells.Add(new SlotCellVm(i, ToggleSlot));
        RefreshCells();
    }

    public string DisplayName => string.IsNullOrWhiteSpace(Name) ? "(unnamed)" : Name;
    public string HotkeyText => Hotkey.Display;
    public string SlotSummary => _selection.Count == 0 ? "no slots" : $"{_selection.Count} slots";

    partial void OnNameChanged(string value)
    {
        Model.Name = value;
        OnPropertyChanged(nameof(DisplayName));
        _persist();
    }

    partial void OnHotkeyChanged(HotkeyToken value)
    {
        Model.Hotkey = value;
        OnPropertyChanged(nameof(HotkeyText));
        _persist();
    }

    private void ToggleSlot(SlotCellVm cell)
    {
        _selection.Toggle(cell.Index);
        Model.SlotOrder = _selection.ToList();
        RefreshCells();
        OnPropertyChanged(nameof(SlotSummary));
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

/// <summary>One clickable inventory slot in the picker.</summary>
public sealed partial class SlotCellVm : ObservableObject
{
    public int Index { get; }
    public int Number => Index + 1;
    public ICommand ToggleCommand { get; }

    [ObservableProperty] private bool _isSelected;
    [ObservableProperty] private int _ordinal;

    public SlotCellVm(int index, Action<SlotCellVm> onToggle)
    {
        Index = index;
        ToggleCommand = new RelayCommand(() => onToggle(this));
    }

    public string OrdinalText => Ordinal > 0 ? Ordinal.ToString() : string.Empty;

    partial void OnOrdinalChanged(int value) => OnPropertyChanged(nameof(OrdinalText));
}
