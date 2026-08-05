using System.Collections.ObjectModel;
using PvPHelper.App.Services;
using PvPHelper.Core.Models;

namespace PvPHelper.App.ViewModels;

/// <summary>A single row in the Dashboard "Bindings" list.</summary>
public sealed class BindingRowVm
{
    public required string Glyph { get; init; }
    public required string Name { get; init; }
    public required string Detail { get; init; }
    public required string HotkeyText { get; init; }
    public bool Enabled { get; set; } = true;
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
            Bindings.Add(new BindingRowVm
            {
                Glyph = "⚔",
                Name = s.Name,
                Detail = $"Auto-switch · {s.SlotOrder.Count} slots",
                HotkeyText = s.Hotkey.Display,
                Enabled = s.Enabled
            });

        foreach (var pr in p.Prayers)
            Bindings.Add(new BindingRowVm
            {
                Glyph = "✦",
                Name = PrayerCatalog.DisplayName(pr.Prayer),
                Detail = "Prayer",
                HotkeyText = pr.Hotkey.Display,
                Enabled = pr.Enabled
            });

        foreach (var sp in p.Specs)
            Bindings.Add(new BindingRowVm
            {
                Glyph = "☄",
                Name = sp.Name,
                Detail = $"Spec · {sp.SpecTimes}× special",
                HotkeyText = sp.Hotkey.Display,
                Enabled = sp.Enabled
            });

        OnPropertyChanged(nameof(BindingsSummary));
        OnPropertyChanged(nameof(CalibrationText));
        OnPropertyChanged(nameof(ClientModeText));
    }
}
