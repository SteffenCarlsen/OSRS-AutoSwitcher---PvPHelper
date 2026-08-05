using PvPHelper.Core.Calibration;
using PvPHelper.Core.Input;
using PvPHelper.Core.Models;

namespace PvPHelper.Core.Automation;

/// <summary>
/// Executes bound actions for a released hotkey, preserving the legacy ordering
/// (prayer → switch → spec). All timings come from the profile's <see cref="AntiBanSettings"/>
/// (and per-spec <see cref="SpecBinding.SpecBarDelayMs"/>) rather than hardcoded constants.
/// Side effects go through <see cref="IInputSender"/> and delays through an injected sleep,
/// so ordering and timing are unit-testable. Returns a short description of what ran, for the
/// activity feed.
/// </summary>
public sealed class ActionEngine
{
    private const int NoWaitSpecDelayMs = 100;

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
    public IReadOnlyList<string> Handle(Profile profile, HotkeyToken key)
    {
        var log = new List<string>();
        if (!key.IsBound) return log;

        var swappedPrayers = HandlePrayers(profile, key, log);
        var swappedItems = HandleSwitches(profile, key, swappedPrayers, log);
        HandleSpecs(profile, key, swappedItems, swappedPrayers, log);
        return log;
    }

    private bool HandlePrayers(Profile p, HotkeyToken key, List<string> log)
    {
        if (!p.Calibration.PrayerReady) return false;
        var ab = p.AntiBan;
        var book = GridGenerator.PrayerBook(p.Calibration.PrayerAnchor!.Value, p.Calibration.PrayerSpacingX, p.Calibration.PrayerSpacingY);

        var swapped = false;
        foreach (var pr in p.Prayers)
        {
            if (!pr.Enabled || pr.Hotkey != key) continue;
            if (!book.TryGetValue(pr.Prayer, out var point)) continue;

            swapped = true;
            var old = _input.GetCursor();
            PressTab(p.Tabs.Prayer);
            _sleep(ab.TabDelayMs);
            SmoothMove(point, ab, click: true);
            if (ab.ReturnToOldPos) SmoothMove(old, ab, click: false);
            log.Add($"✦ {PrayerCatalog.DisplayName(pr.Prayer)}");
        }

        return swapped;
    }

    private bool HandleSwitches(Profile p, HotkeyToken key, bool swappedPrayers, List<string> log)
    {
        if (!p.Calibration.InventoryReady) return false;
        var ab = p.AntiBan;
        var inv = GridGenerator.Inventory(p.Calibration.InventoryAnchor!.Value, p.Calibration.InventorySpacingX, p.Calibration.InventorySpacingY);

        var swapped = false;
        foreach (var sw in p.Switches)
        {
            if (!sw.Enabled || sw.Hotkey != key) continue;
            swapped = true;
            if (swappedPrayers) _sleep(ab.ActionGapMs);

            var old = _input.GetCursor();
            foreach (var slot in sw.SlotOrder)
            {
                if (slot < 0 || slot >= inv.Count) continue;
                PressTab(p.Tabs.Inventory);
                _sleep(ab.TabDelayMs);
                _input.SetCursor(inv[slot]);
                _input.LeftClick(inv[slot]);
                _sleep(ab.SwitchClickDelayMs);
            }

            var ret = sw.ReturnToOldPosOverride ?? ab.ReturnToOldPos;
            if (ret) SmoothMove(old, ab, click: false);
            log.Add($"⚔ {(string.IsNullOrWhiteSpace(sw.Name) ? "Switch" : sw.Name)}");
        }

        return swapped;
    }

    private void HandleSpecs(Profile p, HotkeyToken key, bool swappedItems, bool swappedPrayers, List<string> log)
    {
        if (!p.Calibration.InventoryReady || !p.Calibration.SpecReady) return;
        var ab = p.AntiBan;
        var inv = GridGenerator.Inventory(p.Calibration.InventoryAnchor!.Value, p.Calibration.InventorySpacingX, p.Calibration.InventorySpacingY);

        foreach (var sp in p.Specs)
        {
            if (!sp.Enabled || sp.Hotkey != key) continue;
            if (swappedItems || swappedPrayers) _sleep(ab.ActionGapMs);

            var old = _input.GetCursor();
            PressTab(p.Tabs.Inventory);
            foreach (var slot in sp.WeaponSlots)
            {
                if (slot < 0 || slot >= inv.Count) continue;
                _sleep(ab.TabDelayMs);
                SmoothMove(inv[slot], ab, click: true);
                _sleep(ab.TabDelayMs);
            }

            PressTab(p.Tabs.Attack);
            _sleep(sp.WaitForSpecBar ? sp.SpecBarDelayMs : NoWaitSpecDelayMs);
            SmoothMove(old, ab, click: true);

            for (var i = 0; i < sp.SpecTimes; i++)
            {
                SmoothMove(p.Calibration.SpecBarPoint!.Value, ab, click: true);
                _sleep(ab.SpecClickIntervalMs);
            }

            SmoothMove(old, ab, click: true);
            if (ab.ReturnToOldPos) SmoothMove(old, ab, click: false);
            log.Add($"☄ {(string.IsNullOrWhiteSpace(sp.Name) ? "Spec" : sp.Name)} ×{sp.SpecTimes}");
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
        foreach (var pt in Humanizer.Path(start, end, Math.Max(1, ab.Speed), ab.CurvedPaths, _rng))
        {
            _input.SetCursor(pt);
            _sleep(ab.StepDelayMs);
        }

        _input.SetCursor(end);

        // Only settle + click when we're actually clicking; return moves shouldn't add click delay.
        if (click)
        {
            _sleep(ab.PreClickDelayMs);
            _input.LeftClick(end);
        }
    }
}
