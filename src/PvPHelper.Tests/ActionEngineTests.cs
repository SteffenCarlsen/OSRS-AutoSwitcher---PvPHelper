using PvPHelper.Core.Automation;
using PvPHelper.Core.Input;
using PvPHelper.Core.Models;

namespace PvPHelper.Tests;

public class ActionEngineTests
{
    private sealed class FakeInput : IInputSender
    {
        public List<string> Log { get; } = new();
        private ScreenPoint _cursor;

        public ScreenPoint GetCursor() => _cursor;
        public void SetCursor(ScreenPoint p) { _cursor = p; Log.Add($"move {p.X},{p.Y}"); }
        public void LeftClick(ScreenPoint p) => Log.Add($"click {p.X},{p.Y}");
        public void PressKey(HotkeyToken k) => Log.Add($"key {k.Code}");
    }

    private static Profile BaseProfile()
    {
        var p = new Profile();
        p.AntiBan.ReturnToOldPos = false;
        p.AntiBan.Speed = 1;
        p.AntiBan.JitterRadius = 0;
        p.AntiBan.StepDelayMs = 0;
        p.AntiBan.PreClickDelayMs = 0;
        p.Tabs.Inventory = new HotkeyToken(HotkeySource.Keyboard, 0x71); // F2 = 113
        p.Tabs.Prayer = new HotkeyToken(HotkeySource.Keyboard, 0x73);    // F4 = 115
        p.Tabs.Attack = new HotkeyToken(HotkeySource.Keyboard, 0x70);    // F1 = 112
        p.Calibration.InventoryAnchor = new ScreenPoint(0, 0);
        p.Calibration.InventorySpacingX = 40;
        p.Calibration.InventorySpacingY = 35;
        p.Calibration.PrayerAnchor = new ScreenPoint(500, 500);
        p.Calibration.SpecBarPoint = new ScreenPoint(900, 900);
        return p;
    }

    private static ActionEngine NewEngine(FakeInput fake) =>
        new(fake, sleep: _ => { }, rng: new Random(1));

    [Fact]
    public void Switch_Clicks_Slots_In_Order()
    {
        var fake = new FakeInput();
        var p = BaseProfile();
        var q = new HotkeyToken(HotkeySource.Keyboard, 0x51);
        p.Switches.Add(new SwitchBinding { Name = "s", Hotkey = q, SlotOrder = new() { 0, 1 } });

        NewEngine(fake).Handle(p, q);

        Assert.Contains("key 113", fake.Log);        // inventory tab pressed
        Assert.Contains("click 0,0", fake.Log);      // slot 0 -> (0,0)
        Assert.Contains("click 40,0", fake.Log);     // slot 1 -> (40,0)
        Assert.True(fake.Log.IndexOf("click 0,0") < fake.Log.IndexOf("click 40,0"));
    }

    [Fact]
    public void Combined_Key_Runs_Prayer_Then_Switch_Then_Spec()
    {
        var fake = new FakeInput();
        var p = BaseProfile();
        var k = new HotkeyToken(HotkeySource.Keyboard, 0x51);
        p.Prayers.Add(new PrayerBinding { Prayer = Prayer.ThickSkin, Hotkey = k });
        p.Switches.Add(new SwitchBinding { Name = "s", Hotkey = k, SlotOrder = new() { 0 } });
        p.Specs.Add(new SpecBinding { Name = "sp", Hotkey = k, WeaponSlots = new() { 0 }, SpecTimes = 1 });

        NewEngine(fake).Handle(p, k);

        var prayerTab = fake.Log.IndexOf("key 115");
        var invTab = fake.Log.IndexOf("key 113");
        var attackTab = fake.Log.IndexOf("key 112");

        Assert.True(prayerTab >= 0 && invTab >= 0 && attackTab >= 0);
        Assert.True(prayerTab < invTab, "prayer runs before switch");
        Assert.True(invTab < attackTab, "switch runs before spec");
    }

    [Fact]
    public void Disabled_Binding_Is_Ignored()
    {
        var fake = new FakeInput();
        var p = BaseProfile();
        var q = new HotkeyToken(HotkeySource.Keyboard, 0x51);
        p.Switches.Add(new SwitchBinding { Name = "s", Hotkey = q, SlotOrder = new() { 0 }, Enabled = false });

        NewEngine(fake).Handle(p, q);

        Assert.DoesNotContain("click 0,0", fake.Log);
    }

    [Fact]
    public void NonMatching_Key_Does_Nothing()
    {
        var fake = new FakeInput();
        var p = BaseProfile();
        p.Switches.Add(new SwitchBinding
        {
            Name = "s",
            Hotkey = new HotkeyToken(HotkeySource.Keyboard, 0x51),
            SlotOrder = new() { 0 }
        });

        NewEngine(fake).Handle(p, new HotkeyToken(HotkeySource.Keyboard, 0x45)); // E, unbound

        Assert.Empty(fake.Log);
    }
}
