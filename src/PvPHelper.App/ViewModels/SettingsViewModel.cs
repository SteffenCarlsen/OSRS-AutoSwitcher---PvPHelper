using System;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PvPHelper.App.Services;
using PvPHelper.Core.Input;
using PvPHelper.Core.Models;

namespace PvPHelper.App.ViewModels;

public sealed partial class SettingsViewModel : PageViewModelBase
{
    private readonly SessionState _session;
    private readonly GameWindowLocator _locator;
    private readonly DispatcherTimer _saveDebounce;

    private Profile P => _session.Profile;

    public override string Title => "Settings";
    public override string Subtitle => "Anti-ban, client & input";
    public override string Glyph => "⚙";

    [ObservableProperty]
    private string _windowStatus = string.Empty;

    public SettingsViewModel(SessionState session, GameWindowLocator locator)
    {
        _session = session;
        _locator = locator;
        _saveDebounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
        _saveDebounce.Tick += (_, _) => { _saveDebounce.Stop(); _session.Save(); };
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

    // ---- Input mode ----
    public bool IsForeground { get => P.InputMode == InputMode.Foreground; set { if (value) SetInputMode(InputMode.Foreground); } }
    public bool IsBackground { get => P.InputMode == InputMode.Background; set { if (value) SetInputMode(InputMode.Background); } }

    private void SetInputMode(InputMode mode)
    {
        P.InputMode = mode;
        OnPropertyChanged(nameof(IsForeground));
        OnPropertyChanged(nameof(IsBackground));
        OnPropertyChanged(nameof(InputModeHint));
        Save();
    }

    public string InputModeHint => P.InputMode == InputMode.Background
        ? "Posts window messages to the game — it can stay unfocused. Game-dependent, especially for keys."
        : "Drives the real cursor via SendInput — the game must be the focused window.";

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

    // Re-read every setting from the (possibly hot-swapped) active profile.
    public override void OnNavigatedTo() => OnPropertyChanged(string.Empty);

    private void Save()
    {
        _saveDebounce.Stop();
        _saveDebounce.Start();
    }
}
