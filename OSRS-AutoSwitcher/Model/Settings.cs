using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
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
            ExitKey = Keys.Escape;
        }
        public static Settings Instance => _instance ?? (_instance = new Settings());
        public Keys ExitKey { get; set; }
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

        public static void InitSettings()
        {
            var filePath = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), System.Diagnostics.Process.GetCurrentProcess().ProcessName));
            try
            {
                Program.SettingsDirectory = new DirectoryInfo(filePath);
                if (!Program.SettingsDirectory.Exists)
                {
                    Console.WriteLine("Created settings folder in Appdata");
                    Directory.CreateDirectory(filePath);
                    Program.SettingsDirectory = new DirectoryInfo(filePath);
                    Console.WriteLine();
                }
            }
            catch (DirectoryNotFoundException)
            {
                Console.WriteLine("Created settings folder in Appdata");
                Directory.CreateDirectory(filePath);
                Program.SettingsDirectory = new DirectoryInfo(filePath);
                Console.WriteLine();
            }
        }
    }
}
