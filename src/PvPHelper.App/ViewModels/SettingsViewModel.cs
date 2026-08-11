using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PvPHelper.App.Services;
using PvPHelper.Core.Input;
using PvPHelper.Core.Models;

namespace PvPHelper.App.ViewModels;

/// <summary>A running window the user can target for input.</summary>
public sealed record GameWindowOption(nint Handle, string Title);

public sealed partial class SettingsViewModel : PageViewModelBase
{
    // Known game-client titles, most-specific first — used for the RuneLite default and to keep
    // the persisted title a stable substring (never a player-name-specific full title).
    private static readonly string[] KnownGameTitles = { "RuneLite", "Old School RuneScape", "RuneScape" };

    private readonly SessionState _session;
    private readonly GameWindowLocator _locator;
    private readonly DispatcherTimer _saveDebounce;
    private bool _suppressWindowSelection;

    private Profile P => _session.Profile;

    public override string Title => "Settings";
    public override string Subtitle => "Anti-ban, client & input";
    public override string Glyph => "⚙";

    [ObservableProperty]
    private string _windowStatus = string.Empty;

    /// <summary>All pickable top-level windows (refreshed when the page is shown).</summary>
    public ObservableCollection<GameWindowOption> Windows { get; } = new();

    [ObservableProperty]
    private GameWindowOption? _selectedWindow;

    public SettingsViewModel(SessionState session, GameWindowLocator locator)
    {
        _session = session;
        _locator = locator;
        _saveDebounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
        _saveDebounce.Tick += (_, _) => { _saveDebounce.Stop(); _session.Save(); };
        RefreshWindows();
    }

    partial void OnSelectedWindowChanged(GameWindowOption? value)
    {
        if (_suppressWindowSelection || value is null) return;

        // Use this exact handle for the current session; persist a stable title so the window is
        // re-found automatically next launch.
        _locator.ExplicitWindow = value.Handle;
        TargetWindowTitle = StableTitle(value.Title); // setter calls UseTitle + Save
        WindowStatus = $"Targeting “{value.Title}”";
    }

    /// <summary>Enumerate open windows, then auto-select the saved one, else RuneLite, else none.</summary>
    [RelayCommand]
    private void RefreshWindows()
    {
        _suppressWindowSelection = true;

        Windows.Clear();
        foreach (var w in Win32Windows.EnumerateTopLevel().OrderBy(w => w.Title, StringComparer.OrdinalIgnoreCase))
            Windows.Add(new GameWindowOption(w.Handle, w.Title));

        var saved = P.TargetWindowTitle;
        var pick =
            (!string.IsNullOrWhiteSpace(saved)
                ? Windows.FirstOrDefault(w => w.Title.Contains(saved!, StringComparison.OrdinalIgnoreCase))
                : null)
            ?? Windows.FirstOrDefault(w => w.Title.Contains("RuneLite", StringComparison.OrdinalIgnoreCase));

        _suppressWindowSelection = false;
        SelectedWindow = pick;

        if (pick is not null)
        {
            _locator.ExplicitWindow = pick.Handle;
            WindowStatus = $"Targeting “{pick.Title}”";
        }
        else
        {
            WindowStatus = Windows.Count == 0 ? "No windows found" : "No RuneLite window — pick one below";
        }
    }

    /// <summary>Collapse a full window title to a known game substring so persistence survives
    /// player-name changes ("RuneLite - Zezima" → "RuneLite").</summary>
    private static string StableTitle(string title)
    {
        foreach (var known in KnownGameTitles)
            if (title.Contains(known, StringComparison.OrdinalIgnoreCase))
                return known;
        return title;
    }

    // ---- Anti-ban ----
    public double Speed
    {
        get => P.AntiBan.Speed;
        set { var i = (int)value; if (P.AntiBan.Speed != i) { P.AntiBan.Speed = i; OnPropertyChanged(); Save(); } }
    }

    public bool ReturnToOldPos
    {
        get => P.AntiBan.ReturnToOldPos;
        set { if (P.AntiBan.ReturnToOldPos != value) { P.AntiBan.ReturnToOldPos = value; OnPropertyChanged(); Save(); } }
    }

    public bool CurvedPaths
    {
        get => P.AntiBan.CurvedPaths;
        set { if (P.AntiBan.CurvedPaths != value) { P.AntiBan.CurvedPaths = value; OnPropertyChanged(); Save(); } }
    }

    public double JitterRadius
    {
        get => P.AntiBan.JitterRadius;
        set { var i = (int)value; if (P.AntiBan.JitterRadius != i) { P.AntiBan.JitterRadius = i; OnPropertyChanged(); Save(); } }
    }

    // ---- Timing (advanced, ms) — decimals for the NumericUpDown controls ----
    public decimal StepDelay { get => P.AntiBan.StepDelayMs; set { P.AntiBan.StepDelayMs = (int)value; OnPropertyChanged(); Save(); } }
    public decimal PreClickDelay { get => P.AntiBan.PreClickDelayMs; set { P.AntiBan.PreClickDelayMs = (int)value; OnPropertyChanged(); Save(); } }
    public decimal TabDelay { get => P.AntiBan.TabDelayMs; set { P.AntiBan.TabDelayMs = (int)value; OnPropertyChanged(); Save(); } }
    public decimal ActionGap { get => P.AntiBan.ActionGapMs; set { P.AntiBan.ActionGapMs = (int)value; OnPropertyChanged(); Save(); } }
    public decimal SwitchClickDelay { get => P.AntiBan.SwitchClickDelayMs; set { P.AntiBan.SwitchClickDelayMs = (int)value; OnPropertyChanged(); Save(); } }
    public decimal SpecClickInterval { get => P.AntiBan.SpecClickIntervalMs; set { P.AntiBan.SpecClickIntervalMs = (int)value; OnPropertyChanged(); Save(); } }

    // ---- Client mode ----
    public bool IsFixed { get => P.ClientMode == ClientMode.Fixed; set { if (value) SetClientMode(ClientMode.Fixed); } }
    public bool IsResizable { get => P.ClientMode == ClientMode.Resizable; set { if (value) SetClientMode(ClientMode.Resizable); } }
    public bool IsStretched { get => P.ClientMode == ClientMode.Stretched; set { if (value) SetClientMode(ClientMode.Stretched); } }

    private void SetClientMode(ClientMode mode)
    {
        P.ClientMode = mode;
        OnPropertyChanged(nameof(IsFixed));
        OnPropertyChanged(nameof(IsResizable));
        OnPropertyChanged(nameof(IsStretched));
        Save();
    }

    public double UiScalePercent
    {
        get => P.UiScalePercent;
        set { var i = (int)value; if (P.UiScalePercent != i) { P.UiScalePercent = i; OnPropertyChanged(); Save(); } }
    }

    public string TargetWindowTitle
    {
        get => P.TargetWindowTitle ?? string.Empty;
        set
        {
            var clean = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
            if (P.TargetWindowTitle != clean)
            {
                P.TargetWindowTitle = clean;
                _locator.UseTitle(clean);
                OnPropertyChanged();
                Save();
            }
        }
    }

    [RelayCommand]
    private void CheckWindow()
    {
        var handle = _locator.TargetWindow;
        WindowStatus = handle != 0 ? $"Window found (HWND 0x{handle:X})" : "No matching game window found";
    }

    // ---- Calibration status (read-only for now; overlay lands next) ----
    public string InventoryStatus => P.Calibration.InventoryReady ? "Ready" : "Not set";
    public string PrayerStatus => P.Calibration.PrayerReady ? "Ready" : "Not set";
    public string SpecStatus => P.Calibration.SpecReady ? "Ready" : "Not set";

    // Re-read every setting from the (possibly hot-swapped) active profile, and re-scan windows.
    public override void OnNavigatedTo()
    {
        RefreshWindows();
        OnPropertyChanged(string.Empty);
    }

    private void Save()
    {
        _saveDebounce.Stop();
        _saveDebounce.Start();
    }
}
