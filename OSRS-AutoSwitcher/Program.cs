using System;
using System.IO;
using System.Threading;
using LowLevelInput.Hooks;
using OSRSAutoSwitcher.Features;
using OSRSAutoSwitcher.Interaction;
using OSRSAutoSwitcher.Model;

namespace OSRSAutoSwitcher
{
    class Program
    {
        public static DirectoryInfo SettingsDirectory;
        private static bool _stop = false;
        public static void Main(string[] args)
        {
            Console.Title = "OSRS-AutoSwitcher & PvPHelper";
            Settings.InitSettings();
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
            if (!StartupChecks()) return;
            _stop = false;
            var inputManager = new InputManager(false);
            inputManager.OnKeyboardEvent += InputManagerOnOnKeyboardEvent;
            DateTime startTime = DateTime.Now;
            while (!_stop)
            {
                Thread.Sleep(50);
                var resTime = DateTime.Now - startTime;
                Console.Write("Been running for: " + Math.Floor(resTime.TotalSeconds) + " seconds");
                Console.CursorLeft = 0;
            }

            inputManager.OnKeyboardEvent -= InputManagerOnOnKeyboardEvent;
            inputManager.Dispose();
        }

        private static void InputManagerOnOnKeyboardEvent(VirtualKeyCode key, KeyState state)
        {
            if (state != KeyState.Up) return;

            // Handle prayer state
            bool swappedPrayers = false;
            bool swappedItems = false;

            swappedPrayers = PrayerSwitch. ActivePrayer(key);
            swappedItems = AutoSwitch.ActiveSwitches(key, swappedPrayers);
            SpecialAttack.DoSpecialAttack(key, swappedItems, swappedPrayers);

            var exitKey = LowLevelInput.Converters.KeyCodeConverter.ToVirtualKeyCode((int)Settings.Instance.ExitKey);
            if (exitKey == key)
            {
                _stop = true;
            }
        }

        private static bool StartupChecks()
        {
            Console.Clear();
            if (Settings.Instance == null)
            {
                Console.WriteLine("Settings object was null, please load or set some settings");
                Console.WriteLine("Press any key to return to main menu");
                Console.ReadKey();
                return false;
            }

            var bPrayerActive = Settings.Instance.ActivePrayerHotkeys.Count == 0;
            var bAutoSwitchActive = Settings.Instance.ActiveAutoSwitchHotkeys.Count == 0;
            var bAutoSpecActive = Settings.Instance.SpecialAttackHotkeys.Count == 0;
            if (bPrayerActive && bAutoSwitchActive && bAutoSpecActive)
            {
                Console.WriteLine("Unable to start since no hotkeys were specified");
                Console.WriteLine("Press any key to return to main menu");
                Console.ReadKey();
                return false;
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

            Console.WriteLine($"Press {Settings.Instance.ExitKey} to stop OSRS-AutoSwitcher");
            return true;
        }
    }
}
