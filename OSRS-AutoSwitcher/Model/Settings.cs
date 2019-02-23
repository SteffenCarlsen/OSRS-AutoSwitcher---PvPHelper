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
        public Dictionary<PrayerBook.Prayer,Keys> ActivePrayerHotkeys { get; set; }
        public Dictionary<Keys,List<int>> ActiveAutoSwitchHotkeys { get; set; }
        public Keys PrayerFkey { get; set; }
        public bool WaitForSpecBar { get; set; }
        public Point SpecialAttackBarPoint { get; set; }
        public Dictionary<Keys, SpecialAttackHelper> SpecialAttackHotkeys { get; set; }
    }
}
