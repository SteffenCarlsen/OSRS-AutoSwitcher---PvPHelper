using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PvPHelper.App.Services;
using PvPHelper.Core.Models;

namespace PvPHelper.App.ViewModels;

public sealed partial class PrayersViewModel : PageViewModelBase
{
    private readonly SessionState _session;

    public override string Title => "Prayers";
    public override string Subtitle => "Bind a prayer to a key";
    public override string Glyph => "✦";

    public ObservableCollection<PrayerBindingVm> Bindings { get; } = new();
    public ObservableCollection<PrayerCellVm> Book { get; } = new();

    [ObservableProperty] private PrayerBindingVm? _selected;

    public bool HasSelection => Selected is not null;

    public PrayersViewModel(SessionState session)
    {
        _session = session;
        foreach (var prayer in PrayerCatalog.InBookOrder)
            Book.Add(new PrayerCellVm(prayer, PickPrayer));
        Reload();
    }

    public override void OnNavigatedTo() => Reload();

    private void Reload()
    {
        Bindings.Clear();
        foreach (var model in _session.Profile.Prayers)
            Bindings.Add(new PrayerBindingVm(model, OnBindingChanged));
        Selected = Bindings.FirstOrDefault();
        RefreshBook();
    }

    partial void OnSelectedChanged(PrayerBindingVm? value)
    {
        OnPropertyChanged(nameof(HasSelection));
        RefreshBook();
    }

    private void PickPrayer(PrayerCellVm cell)
    {
        if (Selected is null) AddBinding(cell.Prayer);
        else Selected.Prayer = cell.Prayer; // write-through -> OnBindingChanged -> save + refresh
    }

    private void OnBindingChanged()
    {
        RefreshBook();
        _session.Save();
    }

    private void RefreshBook()
    {
        foreach (var cell in Book)
            cell.IsSelected = Selected is not null && cell.Prayer == Selected.Prayer;
    }

    [RelayCommand]
    private void Add() => AddBinding(Prayer.ProtectFromMelee);

    private void AddBinding(Prayer prayer)
    {
        var model = new PrayerBinding { Prayer = prayer };
        _session.Profile.Prayers.Add(model);
        var vm = new PrayerBindingVm(model, OnBindingChanged);
        Bindings.Add(vm);
        Selected = vm;
        _session.Save();
        RefreshBook();
    }

    [RelayCommand]
    private void Delete(PrayerBindingVm? item)
    {
        if (item is null) return;
        _session.Profile.Prayers.Remove(item.Model);
        Bindings.Remove(item);
        if (ReferenceEquals(Selected, item)) Selected = Bindings.FirstOrDefault();
        _session.Save();
        RefreshBook();
    }
}

public sealed partial class PrayerBindingVm : ObservableObject
{
    private readonly Action _changed;

    public PrayerBinding Model { get; }

    [ObservableProperty] private Prayer _prayer;
    [ObservableProperty] private HotkeyToken _hotkey;

    public PrayerBindingVm(PrayerBinding model, Action changed)
    {
        Model = model;
        _changed = changed;
        _prayer = model.Prayer;
        _hotkey = model.Hotkey;
    }

    public string PrayerName => PrayerCatalog.DisplayName(Prayer);
    public string HotkeyText => Hotkey.Display;

    partial void OnPrayerChanged(Prayer value)
    {
        Model.Prayer = value;
        OnPropertyChanged(nameof(PrayerName));
        _changed();
    }

    partial void OnHotkeyChanged(HotkeyToken value)
    {
        Model.Hotkey = value;
        OnPropertyChanged(nameof(HotkeyText));
        _changed();
    }
}

public sealed partial class PrayerCellVm : ObservableObject
{
    public Prayer Prayer { get; }
    public string Name => PrayerCatalog.DisplayName(Prayer);
    public ICommand PickCommand { get; }

    [ObservableProperty] private bool _isSelected;

    public PrayerCellVm(Prayer prayer, Action<PrayerCellVm> onPick)
    {
        Prayer = prayer;
        PickCommand = new RelayCommand(() => onPick(this));
    }
}
