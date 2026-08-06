using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PvPHelper.App.Services;
using PvPHelper.Core.Input;
using PvPHelper.Core.Models;

namespace PvPHelper.App.ViewModels;

public sealed partial class CalibrationViewModel : PageViewModelBase
{
    private enum Target { Inventory, Prayer, Spec }

    private readonly SessionState _session;
    private readonly IHotkeyHook _hook;
    private Target? _capturing;

    public override string Title => "Calibration";
    public override string Subtitle => "Anchor the inventory, prayers, and spec bar";
    public override string Glyph => "◎";

    [ObservableProperty] private bool _isCapturing;
    [ObservableProperty] private string _status = "Ready. Capture each anchor by hovering the target in-game and pressing any key.";

    public CalibrationViewModel(SessionState session, IHotkeyHook hook)
    {
        _session = session;
        _hook = hook;
    }

    private CalibrationData C => _session.Profile.Calibration;

    public override void OnNavigatedTo() => RaiseStatusProperties();

    // ---- anchor + spacing bindings ----
    public string InventoryAnchorText => C.InventoryAnchor is { } p ? p.ToString() : "not set";
    public string PrayerAnchorText => C.PrayerAnchor is { } p ? p.ToString() : "not set";
    public string SpecAnchorText => C.SpecBarPoint is { } p ? p.ToString() : "not set";

    public double InventorySpacingX { get => C.InventorySpacingX; set { C.InventorySpacingX = (int)value; OnPropertyChanged(); _session.Save(); } }
    public double InventorySpacingY { get => C.InventorySpacingY; set { C.InventorySpacingY = (int)value; OnPropertyChanged(); _session.Save(); } }
    public double PrayerSpacingX { get => C.PrayerSpacingX; set { C.PrayerSpacingX = (int)value; OnPropertyChanged(); _session.Save(); } }
    public double PrayerSpacingY { get => C.PrayerSpacingY; set { C.PrayerSpacingY = (int)value; OnPropertyChanged(); _session.Save(); } }

    [RelayCommand] private void CaptureInventory() => Begin(Target.Inventory);
    [RelayCommand] private void CapturePrayer() => Begin(Target.Prayer);
    [RelayCommand] private void CaptureSpec() => Begin(Target.Spec);

    [RelayCommand]
    private void Cancel() => End("Capture cancelled.");

    private void Begin(Target target)
    {
        if (IsCapturing) return;
        _capturing = target;
        IsCapturing = true;
        _session.IsCalibrating = true;
        Status = $"Hover the {Label(target)} in-game, then press any key. (Esc-free — any key samples the cursor.)";
        _hook.HotkeyReleased += OnCaptureKey;
    }

    private void OnCaptureKey(HotkeyEvent _)
    {
        var point = Win32Cursor.GetPosition(); // physical screen pixels, matches playback
        var target = _capturing;
        Dispatcher.UIThread.Post(() => Apply(target, point));
    }

    private void Apply(Target? target, ScreenPoint point)
    {
        switch (target)
        {
            case Target.Inventory: C.InventoryAnchor = point; break;
            case Target.Prayer: C.PrayerAnchor = point; break;
            case Target.Spec: C.SpecBarPoint = point; break;
            default: return;
        }

        _session.Save();
        End($"Captured {Label(target.Value)} at {point}.");
    }

    private void End(string status)
    {
        _hook.HotkeyReleased -= OnCaptureKey;
        _capturing = null;
        IsCapturing = false;
        _session.IsCalibrating = false;
        Status = status;
        RaiseStatusProperties();
    }

    private static string Label(Target t) => t switch
    {
        Target.Inventory => "first inventory slot (top-left)",
        Target.Prayer => "first prayer (top-left)",
        Target.Spec => "special-attack bar",
        _ => "target"
    };

    private void RaiseStatusProperties()
    {
        OnPropertyChanged(nameof(InventoryAnchorText));
        OnPropertyChanged(nameof(PrayerAnchorText));
        OnPropertyChanged(nameof(SpecAnchorText));
        OnPropertyChanged(nameof(InventorySpacingX));
        OnPropertyChanged(nameof(InventorySpacingY));
        OnPropertyChanged(nameof(PrayerSpacingX));
        OnPropertyChanged(nameof(PrayerSpacingY));
    }
}
