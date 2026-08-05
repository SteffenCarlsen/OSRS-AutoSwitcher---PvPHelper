using PvPHelper.Core.Calibration;
using PvPHelper.Core.Input;
using PvPHelper.Core.Models;

namespace PvPHelper.Core.Automation;

/// <summary>
/// Executes bound actions for a released hotkey, preserving the legacy console ordering
/// (prayer → switch → spec) and timing. Side effects go through <see cref="IInputSender"/>
/// and delays through an injected sleep, so the ordering is unit-testable.
/// </summary>
public sealed class ActionEngine
{
    private readonly IInputSender _input;
    private readonly Action<int> _sleep;
    private readonly Random _rng;

    public ActionEngine(IInputSender input, Action<int>? sleep = null, Random? rng = null)
    {
        _input = input;
        _sleep = sleep ?? System.Threading.Thread.Sleep;
        _rng = rng ?? new Random();
    }

    /// <summary>Resolve every binding that matches the released key and run them in order.</summary>
    public void Handle(Profile profile, HotkeyToken key)
    {
        if (!key.IsBound) return;
        var swappedPrayers = HandlePrayers(profile, key);
        var swappedItems = HandleSwitches(profile, key, swappedPrayers);
        HandleSpecs(profile, key, swappedItems, swappedPrayers);
    }

    private bool HandlePrayers(Profile p, HotkeyToken key)
    {
        if (!p.Calibration.PrayerReady) return false;
        var book = GridGenerator.PrayerBook(p.Calibration.PrayerAnchor!.Value, p.Calibration.PrayerSpacingX, p.Calibration.PrayerSpacingY);

        var swapped = false;
        foreach (var pr in p.Prayers)
        {
            if (!pr.Enabled || pr.Hotkey != key) continue;
            if (!book.TryGetValue(pr.Prayer, out var point)) continue;

            swapped = true;
            var old = _input.GetCursor();
            PressTab(p.Tabs.Prayer);
            _sleep(10);
            SmoothMove(point, p.AntiBan, click: true);
            if (p.AntiBan.ReturnToOldPos) SmoothMove(old, p.AntiBan, click: false);
        }

        return swapped;
    }

    private bool HandleSwitches(Profile p, HotkeyToken key, bool swappedPrayers)
    {
        if (!p.Calibration.InventoryReady) return false;
        var inv = GridGenerator.Inventory(p.Calibration.InventoryAnchor!.Value, p.Calibration.InventorySpacingX, p.Calibration.InventorySpacingY);

        var swapped = false;
        foreach (var sw in p.Switches)
        {
            if (!sw.Enabled || sw.Hotkey != key) continue;
            swapped = true;
            if (swappedPrayers) _sleep(50);

            var old = _input.GetCursor();
            foreach (var slot in sw.SlotOrder)
            {
                if (slot < 0 || slot >= inv.Count) continue;
                PressTab(p.Tabs.Inventory);
                _sleep(10);
                _input.SetCursor(inv[slot]);
                _input.LeftClick(inv[slot]);
                _sleep(50);
            }

            var ret = sw.ReturnToOldPosOverride ?? p.AntiBan.ReturnToOldPos;
            if (ret) SmoothMove(old, p.AntiBan, click: false);
        }

        return swapped;
    }

    private void HandleSpecs(Profile p, HotkeyToken key, bool swappedItems, bool swappedPrayers)
    {
        if (!p.Calibration.InventoryReady || !p.Calibration.SpecReady) return;
        var inv = GridGenerator.Inventory(p.Calibration.InventoryAnchor!.Value, p.Calibration.InventorySpacingX, p.Calibration.InventorySpacingY);

        foreach (var sp in p.Specs)
        {
            if (!sp.Enabled || sp.Hotkey != key) continue;
            if (swappedItems || swappedPrayers) _sleep(50);

            var old = _input.GetCursor();
            PressTab(p.Tabs.Inventory);
            foreach (var slot in sp.WeaponSlots)
            {
                if (slot < 0 || slot >= inv.Count) continue;
                _sleep(10);
                SmoothMove(inv[slot], p.AntiBan, click: true);
                _sleep(10);
            }

            PressTab(p.Tabs.Attack);
            _sleep(sp.WaitForSpecBar ? 450 : 100);
            SmoothMove(old, p.AntiBan, click: true);

            for (var i = 0; i < sp.SpecTimes; i++)
            {
                SmoothMove(p.Calibration.SpecBarPoint!.Value, p.AntiBan, click: true);
                _sleep(100);
            }

            SmoothMove(old, p.AntiBan, click: true);
            if (p.AntiBan.ReturnToOldPos) SmoothMove(old, p.AntiBan, click: false);
        }
    }

    private void PressTab(HotkeyToken tab)
    {
        if (tab.IsBound && tab.Source == HotkeySource.Keyboard)
            _input.PressKey(tab);
    }

    private void SmoothMove(ScreenPoint target, AntiBanSettings ab, bool click)
    {
        var end = Humanizer.Jitter(target, ab.JitterRadius, _rng);
        var start = _input.GetCursor();
        foreach (var pt in Humanizer.Path(start, end, Math.Max(1, ab.Speed)))
        {
            _input.SetCursor(pt);
            _sleep(ab.StepDelayMs);
        }

        _input.SetCursor(end);
        _sleep(ab.PreClickDelayMs);
        if (click) _input.LeftClick(end);
    }
}
