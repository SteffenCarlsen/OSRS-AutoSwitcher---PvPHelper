using PvPHelper.Core.Models;
using PvPHelper.Core.Persistence;

namespace PvPHelper.Tests;

public class ProfileStoreTests
{
    [Fact]
    public void Save_Then_Load_RoundTrips()
    {
        var dir = Path.Combine(Path.GetTempPath(), "pvph-tests-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new ProfileStore(dir);
            var profile = new Profile { Name = "RoundTrip", ClientMode = ClientMode.Stretched };
            profile.Switches.Add(new SwitchBinding
            {
                Name = "Full Rune",
                Hotkey = new HotkeyToken(HotkeySource.Keyboard, 0x51), // Q
                SlotOrder = new List<int> { 2, 6, 10 }
            });
            profile.Prayers.Add(new PrayerBinding
            {
                Prayer = Prayer.ProtectFromMelee,
                Hotkey = new HotkeyToken(HotkeySource.Mouse, 4)
            });
            profile.Calibration.InventoryAnchor = new ScreenPoint(10, 20);

            store.Save(profile);

            Assert.Contains("RoundTrip", store.ListNames());

            var loaded = store.Load("RoundTrip");
            Assert.NotNull(loaded);
            Assert.Equal(ClientMode.Stretched, loaded!.ClientMode);
            Assert.Single(loaded.Switches);
            Assert.Equal("Full Rune", loaded.Switches[0].Name);
            Assert.Equal(new List<int> { 2, 6, 10 }, loaded.Switches[0].SlotOrder);
            Assert.Equal(new HotkeyToken(HotkeySource.Keyboard, 0x51), loaded.Switches[0].Hotkey);
            Assert.Equal(Prayer.ProtectFromMelee, loaded.Prayers[0].Prayer);
            Assert.Equal(new HotkeyToken(HotkeySource.Mouse, 4), loaded.Prayers[0].Hotkey);
            Assert.Equal(new ScreenPoint(10, 20), loaded.Calibration.InventoryAnchor);
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
    }
}
