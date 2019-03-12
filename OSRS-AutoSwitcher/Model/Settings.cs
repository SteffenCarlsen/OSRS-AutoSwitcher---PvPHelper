using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using OSRSAutoSwitcher.Features;

namespace OSRSAutoSwitcher.Model
{
    /// <summary>
    /// Singleton Settings instance for saving the user preferences
    /// </summary>
    public class Settings
    {
        private static Settings _instance;

        public Settings()
        {
            Prayers = new Dictionary<PrayerBook.Prayer, Point>();
            ActiveAutoSwitchHotkeys = new Dictionary<Keys, List<int>>();
            ActivePrayerHotkeys = new Dictionary<PrayerBook.Prayer, Keys>();
            SpecialAttackHotkeys = new Dictionary<Keys, SpecialAttackHelper>();
        }
        public static Settings Instance => _instance ?? (_instance = new Settings());
        public int Speed { get; set; }
        public bool ReturnToOldPos { get; set; }
        public bool Stretched { get; set; }
        public List<Point> InventorySlots { get; set; }
        public Keys AttackFkey { get; set; }
        public Keys InventFKey { get; set; }
        public Dictionary<PrayerBook.Prayer, Point> Prayers { get; set; }
        public Dictionary<PrayerBook.Prayer, Keys> ActivePrayerHotkeys { get; set; }
        public Dictionary<Keys, List<int>> ActiveAutoSwitchHotkeys { get; set; }
        public Keys PrayerFkey { get; set; }
        public bool WaitForSpecBar { get; set; }
        public Point SpecialAttackBarPoint { get; set; }
        public Dictionary<Keys, SpecialAttackHelper> SpecialAttackHotkeys { get; set; }

        public bool IsValid(out bool bPrayerActive, out bool bAutoSwitchActive, out bool bAutoSpecActive)
        {
            bPrayerActive = Settings.Instance.ActivePrayerHotkeys.Count > 0;
            bAutoSwitchActive = Settings.Instance.ActiveAutoSwitchHotkeys.Count > 0;
            bAutoSpecActive = Settings.Instance.SpecialAttackHotkeys.Count > 0;
            if (bPrayerActive | bAutoSwitchActive | bAutoSpecActive)
            {
                return true;
            }
            Console.WriteLine("Settings object was null, please load or set some settings");
            Console.WriteLine("Press any key to return to main menu");
            Console.ReadKey();
            return false;
        }

        public void PrettyPrint()
        {
            Console.Clear();
            Console.WriteLine("Mousespeed is: " + Settings.Instance.Speed);
            Console.WriteLine("Return to old pos: " + Settings.Instance.ReturnToOldPos);
            foreach (var autoSwitchHotkey in Settings.Instance.ActiveAutoSwitchHotkeys)
            {
                Console.WriteLine("Autoswitch on hotkey: " + autoSwitchHotkey.Key);
            }
            foreach (var prayerHotkey in Settings.Instance.ActivePrayerHotkeys)
            {
                Console.WriteLine(prayerHotkey.Key + " on hotkey " + prayerHotkey.Value);
            }

            foreach (var spec in Settings.Instance.SpecialAttackHotkeys)
            {
                Console.WriteLine("Special attack on hotkey: " + spec.Key);
            }
        }
    }
}
