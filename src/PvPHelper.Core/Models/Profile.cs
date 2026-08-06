namespace PvPHelper.Core.Models;

/// <summary>OSRS client layout the coordinates were calibrated against.</summary>
public enum ClientMode
{
    Fixed,
    Resizable,
    Stretched
}

/// <summary>Movement/humanisation knobs. Defaults reproduce the legacy console feel.</summary>
public sealed class AntiBanSettings
{
    /// <summary>Number of interpolation steps per mouse move — lower is faster.</summary>
    public int Speed { get; set; } = 12;

    /// <summary>Return the cursor to its original position after an action.</summary>
    public bool ReturnToOldPos { get; set; } = true;

    /// <summary>Random +/- pixel radius applied to click targets.</summary>
    public int JitterRadius { get; set; } = 5;

    /// <summary>Delay between interpolation steps (ms).</summary>
    public int StepDelayMs { get; set; } = 10;

    /// <summary>Settle delay before a click (ms).</summary>
    public int PreClickDelayMs { get; set; } = 50;

    /// <summary>Delay after pressing an in-game tab F-key before clicking (ms).</summary>
    public int TabDelayMs { get; set; } = 10;

    /// <summary>Gap inserted between combined prayer / switch / spec actions on one key (ms).</summary>
    public int ActionGapMs { get; set; } = 50;

    /// <summary>Delay after each inventory click during a switch (ms).</summary>
    public int SwitchClickDelayMs { get; set; } = 50;

    /// <summary>Delay between successive special-attack clicks (ms).</summary>
    public int SpecClickIntervalMs { get; set; } = 100;

    /// <summary>Use a slight curve instead of a straight line.</summary>
    public bool CurvedPaths { get; set; }
}

/// <summary>The in-game tab hotkeys used to open panels before clicking.</summary>
public sealed class TabKeys
{
    public HotkeyToken Inventory { get; set; } = HotkeyToken.None;
    public HotkeyToken Prayer { get; set; } = HotkeyToken.None;
    public HotkeyToken Attack { get; set; } = HotkeyToken.None;
}

/// <summary>Captured anchor points + grid spacing. Grids are regenerated from these.</summary>
public sealed class CalibrationData
{
    public ScreenPoint? InventoryAnchor { get; set; }
    public int InventorySpacingX { get; set; } = 40;
    public int InventorySpacingY { get; set; } = 35;

    public ScreenPoint? PrayerAnchor { get; set; }
    public int PrayerSpacingX { get; set; } = 40;
    public int PrayerSpacingY { get; set; } = 40;

    public ScreenPoint? SpecBarPoint { get; set; }

    public bool InventoryReady => InventoryAnchor.HasValue;
    public bool PrayerReady => PrayerAnchor.HasValue;
    public bool SpecReady => SpecBarPoint.HasValue;
    public int ReadyCount => (InventoryReady ? 1 : 0) + (PrayerReady ? 1 : 0) + (SpecReady ? 1 : 0);
}

public abstract class BindingBase
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public HotkeyToken Hotkey { get; set; } = HotkeyToken.None;
    public bool Enabled { get; set; } = true;
}

/// <summary>Equipment switch — clicks a set of inventory slots in order.</summary>
public sealed class SwitchBinding : BindingBase
{
    /// <summary>0-based inventory slot indices, in click order.</summary>
    public List<int> SlotOrder { get; set; } = new();

    /// <summary>Per-binding override of the global "return to old position".</summary>
    public bool? ReturnToOldPosOverride { get; set; }
}

/// <summary>Prayer toggle bound to a key.</summary>
public sealed class PrayerBinding : BindingBase
{
    public Prayer Prayer { get; set; }
}

/// <summary>Special-attack helper — equip a spec weapon and spec the target N times.</summary>
public sealed class SpecBinding : BindingBase
{
    public List<int> WeaponSlots { get; set; } = new();
    public bool WaitForSpecBar { get; set; }
    public int SpecTimes { get; set; } = 1;

    /// <summary>Delay after activating the special attack before clicking the target, when
    /// <see cref="WaitForSpecBar"/> is on — time for the spec bar to appear (ms).</summary>
    public int SpecBarDelayMs { get; set; } = 450;
}

/// <summary>A complete, named preset. One profile = one saved JSON file.</summary>
public sealed class Profile
{
    public string Name { get; set; } = "New Profile";
    public int Version { get; set; } = 1;
    public ClientMode ClientMode { get; set; } = ClientMode.Fixed;
    public int UiScalePercent { get; set; } = 100;

    /// <summary>Optional window-title substring used to locate the game window for input.</summary>
    public string? TargetWindowTitle { get; set; }

    public AntiBanSettings AntiBan { get; set; } = new();
    public TabKeys Tabs { get; set; } = new();
    public CalibrationData Calibration { get; set; } = new();

    public HotkeyToken ArmHotkey { get; set; } = new(HotkeySource.Keyboard, 0x77); // F8
    public HotkeyToken PanicKey { get; set; } = new(HotkeySource.Keyboard, 0x1B);   // Esc

    public List<SwitchBinding> Switches { get; set; } = new();
    public List<PrayerBinding> Prayers { get; set; } = new();
    public List<SpecBinding> Specs { get; set; } = new();

    public int BindingCount => Switches.Count + Prayers.Count + Specs.Count;
}
