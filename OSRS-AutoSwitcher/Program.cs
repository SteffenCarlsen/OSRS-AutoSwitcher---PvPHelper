using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using OSRSAutoSwitcher.Features;
using OSRSAutoSwitcher.Globals;
using OSRSAutoSwitcher.Interaction;
using OSRSAutoSwitcher.Model;

namespace OSRSAutoSwitcher
{
    class Program
    {
        public static DirectoryInfo SettingsDirectory;
        public static void Main(string[] args)
        {
            Console.Title = "OSRS-AutoSwitcher & PvPHelper";
            InitSettings();
            while (true)
            {
                CLI.RenderMenu();
                var input = Console.ReadKey().Key;
                Console.WriteLine();
                switch (input)
                {
                    case ConsoleKey.D0:
                        CLI.EditSettings();
                        break;
                    case ConsoleKey.D1:
                        CLI.AutoswitchSettings();
                        break;
                    case ConsoleKey.D2:
                        CLI.InitializePrayer();
                        break;
                    case ConsoleKey.D3:
                        CLI.SetupSpecHelper();
                        break;
                    case ConsoleKey.D4:
                        CLI.SaveSettings();
                        break;
                    case ConsoleKey.D5:
                        CLI.LoadSettings();
                        break;
                    case ConsoleKey.D6:
                        Start();
                        break;
                    case ConsoleKey.D7:
                        CLI.HotkeySettings();
                        break;
                    case ConsoleKey.D8:
                        Environment.Exit(0);
                        break;
                    case ConsoleKey.D9:
                        break;
                    default:
                        break;
                }

            }
        }

        private static void Start()
        {
            Console.Clear();
            if (Settings.Instance == null)
            {
                Console.WriteLine("Settings object was null, please load or set some settings");
                Console.WriteLine("Press any key to return to main menu");
                Console.ReadKey();
                return;
            }
            var bPrayerActive = Settings.Instance.ActivePrayerHotkeys == null;
            var bAutoSwitchActive = Settings.Instance.ActiveAutoSwitchHotkeys == null;
            var bAutoSpecActive = Settings.Instance.SpecialAttackHotkeys == null;
            if (bPrayerActive && bAutoSwitchActive)
            {
                Console.WriteLine("Unable to start since no hotkeys were specified");
                Console.WriteLine("Press any key to return to main menu");
                Console.ReadKey();
                return;
            }

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
            Console.WriteLine("Press ESC to stop OSRS-AutoSwitcher");
            DateTime startTime = DateTime.Now;
            while (true)
            {
                Thread.Sleep(50);
                
                var resTime = DateTime.Now - startTime;
                Console.Write("Been running for: " + Math.Floor(resTime.TotalSeconds) + " seconds");
                Console.CursorLeft = 0;
                if (!bPrayerActive)
                {
                    foreach (var prayerHotkey in Settings.Instance.ActivePrayerHotkeys)
                    {
                        if (Convert.ToBoolean((long) WindowsImports.GetAsyncKeyState(prayerHotkey.Value) & 0x8000))
                        {
                            var oldPos = Cursor.Position;
                            SendKeys.SendWait("{" + Settings.Instance.PrayerFkey + "}");
                            Thread.Sleep(10);
                            Mouse.LinearSmoothMove(Settings.Instance.Prayers[prayerHotkey.Key], Settings.Instance.Speed, true);
                            if (Settings.Instance.ReturnToOldPos)
                            {
                                Mouse.LinearSmoothMove(oldPos,Settings.Instance.Speed,false);
                            }
                        }
                    }
                }

                if (!bAutoSwitchActive)
                {
                    foreach (var autoSwitchHotkey in Settings.Instance.ActiveAutoSwitchHotkeys)
                    {
                        if (Convert.ToBoolean((long) WindowsImports.GetAsyncKeyState(autoSwitchHotkey.Key) & 0x8000))
                        {
                            if (Settings.Instance.ActiveAutoSwitchHotkeys != null)
                            {
                                var oldPos = Cursor.Position;
                                foreach (var locations in autoSwitchHotkey.Value)
                                {
                                    SendKeys.SendWait("{" + Settings.Instance.InventFKey + "}");
                                    Thread.Sleep(10);
                                    WindowsImports.SetCursorPos(Settings.Instance.InventorySlots[locations].X,
                                        Settings.Instance.InventorySlots[locations].Y);
                                    WindowsImports.LeftMouseClick(Settings.Instance.InventorySlots[locations].X,
                                        Settings.Instance.InventorySlots[locations].Y);
                                    Thread.Sleep(50);
                                }
                                
                                if (Settings.Instance.ReturnToOldPos)
                                {
                                    Mouse.LinearSmoothMove(oldPos, Settings.Instance.Speed, false);
                                }
                            }
                            else
                            {
                                Console.WriteLine("Autoswitch inventory settings was null, terminating...");
                                return;
                            }

                        }
                    }
                }

                if (!bAutoSpecActive)
                {
                    if (Settings.Instance.SpecialAttackHotkeys != null)
                    {
                        foreach (var hotkey in Settings.Instance.SpecialAttackHotkeys)
                        {
                            if (Convert.ToBoolean((long) WindowsImports.GetAsyncKeyState(hotkey.Key) & 0x8000))
                            {
                                var oldPos = Cursor.Position;
                                SendKeys.SendWait("{" + Settings.Instance.InventFKey + "}");
                                foreach (var specWep in hotkey.Value.InventoryId)
                                {
                                    Thread.Sleep(10);
                                    Mouse.LinearSmoothMove(new Point(
                                            Settings.Instance.InventorySlots[specWep].X,
                                            Settings.Instance.InventorySlots[specWep].Y),
                                            Settings.Instance.Speed,
                                            true);
                                    Thread.Sleep(10);
                                }

                                SendKeys.SendWait("{" + Settings.Instance.AttackFkey + "}");
                                Thread.Sleep(Settings.Instance.WaitForSpecBar ? 450 : 100);
                                Mouse.LinearSmoothMove(oldPos,Settings.Instance.Speed, true);
                                int pressedCounter = 0;
                                while (pressedCounter != hotkey.Value.SpecTimes)
                                {
                                    Mouse.LinearSmoothMove(Settings.Instance.SpecialAttackBarPoint,
                                        Settings.Instance.Speed, true);
                                    Thread.Sleep(100);
                                    pressedCounter++;
                                }
                                Mouse.LinearSmoothMove(oldPos,
                                    Settings.Instance.Speed, true);
                                if (Settings.Instance.ReturnToOldPos)
                                {
                                    Mouse.LinearSmoothMove(oldPos, Settings.Instance.Speed, false);
                                }

                            }
                        }
                    }

                    if (Convert.ToBoolean((long) WindowsImports.GetAsyncKeyState(Keys.Escape) & 0x0001))
                    {
                        break;
                    }


                }
            }
        }

        private static void InitSettings()
        {
            var filePath = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),System.Diagnostics.Process.GetCurrentProcess().ProcessName));
            try
            {
                SettingsDirectory = new DirectoryInfo(filePath);
                if (!SettingsDirectory.Exists)
                {
                    Console.WriteLine("Created settings folder in Appdata");
                    Directory.CreateDirectory(filePath);
                    SettingsDirectory = new DirectoryInfo(filePath);
                    Console.WriteLine();
                }
            }
            catch (DirectoryNotFoundException)
            {
                Console.WriteLine("Created settings folder in Appdata");
                Directory.CreateDirectory(filePath);
                SettingsDirectory = new DirectoryInfo(filePath);
                Console.WriteLine();
            }
        }
    }
}
