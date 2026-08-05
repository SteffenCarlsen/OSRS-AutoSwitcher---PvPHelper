using PvPHelper.Core.Models;

namespace PvPHelper.App.Services;

/// <summary>
/// A populated demo profile so the Dashboard looks alive before real profiles exist.
/// Replaced by first-run / ProfileStore loading in a later phase.
/// </summary>
public static class SampleData
{
    public static Profile CreateDefaultProfile()
    {
        var p = new Profile
        {
            Name = "Main PKer",
            ClientMode = ClientMode.Fixed,
            UiScalePercent = 100
        };

        p.Tabs.Inventory = new HotkeyToken(HotkeySource.Keyboard, 0x71); // F2
        p.Tabs.Prayer = new HotkeyToken(HotkeySource.Keyboard, 0x73);    // F4
        p.Tabs.Attack = new HotkeyToken(HotkeySource.Keyboard, 0x70);    // F1

        p.Calibration.InventoryAnchor = new ScreenPoint(1418, 728);
        p.Calibration.PrayerAnchor = new ScreenPoint(1418, 520);
        p.Calibration.SpecBarPoint = new ScreenPoint(1505, 806);

        p.Switches.Add(new SwitchBinding
        {
            Name = "Full Rune",
            Hotkey = new HotkeyToken(HotkeySource.Keyboard, 0x51), // Q
            SlotOrder = new() { 2, 6, 10, 14, 18 }
        });
        p.Switches.Add(new SwitchBinding
        {
            Name = "Mage Gear",
            Hotkey = new HotkeyToken(HotkeySource.Keyboard, 0x45), // E
            SlotOrder = new() { 0, 1, 4, 5 }
        });
        p.Switches.Add(new SwitchBinding
        {
            Name = "Tank Switch",
            Hotkey = new HotkeyToken(HotkeySource.Keyboard, 0x33), // 3
            SlotOrder = new() { 3, 7, 11 }
        });

        p.Prayers.Add(new PrayerBinding
        {
            Prayer = Prayer.ProtectFromMelee,
            Hotkey = new HotkeyToken(HotkeySource.Keyboard, 0x46) // F
        });
        p.Prayers.Add(new PrayerBinding
        {
            Prayer = Prayer.Smite,
            Hotkey = new HotkeyToken(HotkeySource.Keyboard, 0x34) // 4
        });

        p.Specs.Add(new SpecBinding
        {
            Name = "AGS Spec",
            Hotkey = new HotkeyToken(HotkeySource.Keyboard, 0x52), // R
            WeaponSlots = new() { 1 },
            WaitForSpecBar = true,
            SpecTimes = 2
        });

        return p;
    }
}
