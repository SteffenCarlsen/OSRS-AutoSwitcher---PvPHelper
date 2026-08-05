using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using PvPHelper.App.Services;
using PvPHelper.Core.Models;

namespace PvPHelper.App.ViewModels;

/// <summary>A single row in the Dashboard "Bindings" list; the toggle writes back to the model.</summary>
public sealed partial class BindingRowVm : ObservableObject
{
    private readonly Action<bool> _onEnabledChanged;

    public string Glyph { get; }
    public string Name { get; }
    public string Detail { get; }
    public string HotkeyText { get; }

    [ObservableProperty] private bool _enabled;

    public BindingRowVm(string glyph, string name, string detail, string hotkeyText, bool enabled, Action<bool> onEnabledChanged)
    {
        Glyph = glyph;
        Name = name;
        Detail = detail;
        HotkeyText = hotkeyText;
        _enabled = enabled;
        _onEnabledChanged = onEnabledChanged;
    }

    partial void OnEnabledChanged(bool value) => _onEnabledChanged(value);
}

public sealed class DashboardViewModel : PageViewModelBase
{
    public SessionState Session { get; }

    public override string Title => "Dashboard";
    public override string Subtitle => "Command center";

    public ObservableCollection<BindingRowVm> Bindings { get; } = new();

    public string BindingsSummary =>
        $"{Session.Profile.BindingCount}   ·   {Session.Profile.Switches.Count} switch / {Session.Profile.Prayers.Count} prayer / {Session.Profile.Specs.Count} spec";
    public string CalibrationText => $"{Session.Profile.Calibration.ReadyCount} / 3";
    public string ClientModeText => $"{Session.Profile.ClientMode} · {Session.Profile.UiScalePercent}%";

    public DashboardViewModel(SessionState session)
    {
        Session = session;
        Rebuild();
    }

    public override void OnNavigatedTo() => Rebuild();

    private void Rebuild()
    {
        var p = Session.Profile;
        Bindings.Clear();

        foreach (var s in p.Switches)
            Bindings.Add(new BindingRowVm("⚔", s.Name, $"Auto-switch · {s.SlotOrder.Count} slots",
                s.Hotkey.Display, s.Enabled, e => { s.Enabled = e; Session.Save(); }));

        foreach (var pr in p.Prayers)
            Bindings.Add(new BindingRowVm("✦", PrayerCatalog.DisplayName(pr.Prayer), "Prayer",
                pr.Hotkey.Display, pr.Enabled, e => { pr.Enabled = e; Session.Save(); }));

        foreach (var sp in p.Specs)
            Bindings.Add(new BindingRowVm("☄", sp.Name, $"Spec · {sp.SpecTimes}× special",
                sp.Hotkey.Display, sp.Enabled, e => { sp.Enabled = e; Session.Save(); }));

        OnPropertyChanged(nameof(BindingsSummary));
        OnPropertyChanged(nameof(CalibrationText));
        OnPropertyChanged(nameof(ClientModeText));
    }
}
