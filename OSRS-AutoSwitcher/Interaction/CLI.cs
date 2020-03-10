using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using OSRSAutoSwitcher.Features;
using OSRSAutoSwitcher.Globals;
using OSRSAutoSwitcher.Model;

namespace OSRSAutoSwitcher.Interaction
{
    public static class CLI
    {
        public static List<Point> GetInventory()
        {
            Console.WriteLine("Please press 'A' while hovering over the first inventory slot");
            while (Console.ReadKey().Key != ConsoleKey.A)
            {
                Thread.Yield();
            }
            var cursorPos = Cursor.Position;
            Console.WriteLine("\nFirst item slot position: " + cursorPos);
            var inventory = AutoSwitch.GenerateInventory(cursorPos);
            return inventory;
        }

        public static void LoadSettings()
        {
            var settings = Settings.Instance;
            Console.Clear();
            FileInfo[] SettingsFiles = new FileInfo[] { };
            int counter = 1;
            SettingsFiles = Program.SettingsDirectory.GetFiles("*.json");
            if (SettingsFiles.Length > 0)
            {
                foreach (var file in SettingsFiles)
                {
                    Console.WriteLine(counter + ". " + file.Name);
                    counter++;
                }

                Console.WriteLine(counter + ". Return to main menu");
                Console.WriteLine("Which settings do you wish to load?");
                if (Int32.TryParse(Console.ReadLine(), out var value))
                {
                    if (value < counter)
                    {
                        settings = JsonObjectFileSaveLoad.ReadFromJsonFile<Settings>(SettingsFiles[value - 1].Name);
                        Settings.Instance.ActiveAutoSwitchHotkeys = settings.ActiveAutoSwitchHotkeys;
                        Settings.Instance.ActivePrayerHotkeys = settings.ActivePrayerHotkeys;
                        Settings.Instance.AttackFkey = settings.AttackFkey;
                        Settings.Instance.InventorySlots = settings.InventorySlots;
                        Settings.Instance.PrayerFkey = settings.PrayerFkey;
                        Settings.Instance.SpecialAttackBarPoint = settings.SpecialAttackBarPoint;
                        Settings.Instance.SpecialAttackHotkeys = settings.SpecialAttackHotkeys;
                        Settings.Instance.Speed = settings.Speed;
                        Settings.Instance.ReturnToOldPos = settings.ReturnToOldPos;
                        Settings.Instance.Prayers = settings.Prayers;
                        Settings.Instance.Stretched = settings.Stretched;
                        Settings.Instance.InventFKey = settings.InventFKey;
                        Settings.Instance.WaitForSpecBar = settings.WaitForSpecBar;
                        Console.WriteLine("Loaded profile stored in: " + SettingsFiles[value - 1].Name);
                        Console.WriteLine("Press any key to return to main menu");
                        Console.ReadKey();

                    }
                    else if (value == counter)
                    {
                        return;
                    }
                    else
                    {
                        Console.WriteLine("Invalid value");
                        Console.WriteLine("Press any key to return to main menu");
                        Console.ReadKey();
                    }
                }
                else
                {
                    Console.WriteLine("Invalid value");
                    Console.WriteLine("Press any key to return to main menu");
                    Console.ReadKey();
                }
            }
            else
            {
                Console.WriteLine("None");
                Console.WriteLine("Press any key to return to main menu");
                Console.ReadKey();
            }
        }

        public static void SaveSettings()
        {
            Console.Clear();
            FileInfo[] settingsFiles = Program.SettingsDirectory.GetFiles("*.json");
            if (settingsFiles.Length > 0)
            {
                int counterA = 0;
                foreach (var setting in settingsFiles)
                {
                    counterA++;
                    Console.WriteLine(counterA + ". Override " + setting.Name);
                }

                Console.WriteLine(counterA + 1 + ". New settings");
                Console.WriteLine(counterA + 2 + ". Return to main menu");
                Console.WriteLine("How do you wish to save your settings? ");
                if (Int32.TryParse(Console.ReadLine(), out var value))
                {
                    if (value == counterA + 1)
                    {
                        Console.WriteLine("Write the name of the new settings");
                        var name = Console.ReadLine();
                        JsonObjectFileSaveLoad.WriteToJsonFile(name, Settings.Instance);
                        Console.WriteLine("Successfully saved the settings with name \"" + name + "\"");
                        Console.WriteLine("Press any key to return to main menu");
                        Console.ReadKey();
                    }
                    else if (value == counterA + 2)
                    {
                        return;
                    }
                    else
                    {
                        var fileName = Path.GetFileNameWithoutExtension(settingsFiles[value - 1].Name);
                        JsonObjectFileSaveLoad.WriteToJsonFile(fileName, Settings.Instance);
                        Console.WriteLine("Overwritten " + fileName);
                        Console.WriteLine("Press any key to return to main menu");
                        Console.ReadKey();
                    }
                }
                else
                {
                    Console.WriteLine("Invalid value");
                    Console.WriteLine("Press any key to return to main menu");
                    Console.ReadKey();
                }
            }
            else
            {
                Console.WriteLine("No settings currently exist");
                Console.WriteLine("Write the name of the new settings");
                var name = Console.ReadLine();
                JsonObjectFileSaveLoad.WriteToJsonFile(name, Settings.Instance);
                Console.WriteLine("Successfully saved the settings with name \"" + name + "\"");
                Console.WriteLine("Press any key to return to main menu");
                Console.ReadKey();
            }
        }

        public static void RenderSettings(Settings settings)
        {
            Console.WriteLine("1. Mouse speed(lower = faster): " + settings.Speed);
            Console.WriteLine("2. Return to old mouse position after switching: " + settings.ReturnToOldPos);
            Console.WriteLine("3. Return to main menu");
        }

        public static void RenderMenu()
        {
            Console.Clear();;
            Console.WriteLine("Runescape PvPHelper v" + GetVersion());
            Console.WriteLine("0. Antiban settings");
            Console.WriteLine("1. Setup Autoswitcher");
            Console.WriteLine("2. Setup AutoPrayer");
            Console.WriteLine("3. Setup SpecHelper");
            Console.WriteLine("4. Save Settings");
            Console.WriteLine("5. Load Settings");
            Console.WriteLine("6. Start PvPHelper");
            Console.WriteLine("7. Setup hotkeys");
            Console.WriteLine("8. Exit PvPHelper");
            Console.WriteLine("Please input a number to continue...");
        }

        private static string GetVersion()
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            string version = fvi.FileVersion;
            return version;
        }

        public static Dictionary<PrayerBook.Prayer, Point> GetPrayers()
        {
            Console.WriteLine("Please press 'A' while hovering over the first prayer slot");
            while (Console.ReadKey().Key != ConsoleKey.A)
            {
                Thread.Yield();
            }
            var cursorPos = Cursor.Position;
            Console.WriteLine("\nFirst prayer slot position: " + cursorPos);
            var prayers = PrayerSwitch.GeneratePrayerBook(cursorPos, 40, 40);
            return prayers;
        }
        public static void InitializePrayer()
        {
            Console.WriteLine();
            Settings.Instance.PrayerFkey = CLI.GetPrayerKey();
            Console.WriteLine("\nYour prayer F-key is: " + Settings.Instance.PrayerFkey);
            Console.WriteLine();
            Settings.Instance.Prayers = CLI.GetPrayers();
            if (Settings.Instance.Prayers.Count > 0)
            {
                Console.WriteLine("Successfully saved prayer data");
            }


            int count = Enum.GetValues(typeof(PrayerBook.Prayer)).Length;
            bool bContinue = true;
            var values = Enum.GetValues(typeof(PrayerBook.Prayer)).Cast<PrayerBook.Prayer>();
            while (bContinue)
            {
                int c = 0;
                int choice = 0;
                foreach (var prayers in values)
                {
                    Console.WriteLine(c + ". " + prayers.ToString());
                    c++;
                }

                Console.WriteLine(c + ". Return to main menu");
                Console.WriteLine("Select prayer to hotkey");
                if (Int32.TryParse(Console.ReadLine(), out choice))
                {
                    if (choice < count)
                    {
                        Console.WriteLine("Please press on your desired prayer switch key");
                        var tempKey = (Keys)Console.ReadKey().Key;
                        if (Settings.Instance.ActivePrayerHotkeys == null)
                        {
                            Settings.Instance.ActivePrayerHotkeys = new Dictionary<PrayerBook.Prayer, Keys>();
                        }
                        Settings.Instance.ActivePrayerHotkeys.Add(PrayerBook.GetNextPrayer(choice), tempKey);
                        Console.WriteLine("Saved prayer hotkey");
                        Console.WriteLine("Press any key to continue setting prayers");
                        Console.ReadKey();
                    }
                    else if (choice == count)
                    {
                        bContinue = false;
                    }
                }
                else
                {
                    bContinue = false;
                    Console.WriteLine("Press any key to return to main menu");
                    Console.ReadKey();
                }
            }

            Console.WriteLine();
        }

        public static Keys GetPrayerKey()
        {
            Console.Clear();
            Console.WriteLine("Please press on your prayer F-key");
            return (Keys)Console.ReadKey().Key;
        }

        public static List<int> GetAutoSwitchItems(bool status = true)
        {
            return AutoSwitch.AutoswitchItems(status);
        }

        public static void EditSettings()
        {
            resetSettings:
            Console.Clear();
            CLI.RenderSettings(Settings.Instance);
            Console.WriteLine("Enter a setting to change");
            var KeyToSwitch = (Keys)Console.ReadKey().Key;
            if (KeyToSwitch == Keys.D1 || KeyToSwitch == Keys.NumPad1)
            {
                Console.Clear();
                Console.WriteLine("Mouse speed(lower = faster): ");
                var succParse = int.TryParse(Console.ReadLine(), out var newSpeed);
                if (succParse)
                {
                    Settings.Instance.Speed = newSpeed;
                }
                else
                {
                    Console.WriteLine("invalid value");
                }
            }
            else if (KeyToSwitch == Keys.D2 || KeyToSwitch == Keys.NumPad2)
            {
                Settings.Instance.ReturnToOldPos = !Settings.Instance.ReturnToOldPos;
            }
            else if (KeyToSwitch == Keys.D3 || KeyToSwitch == Keys.NumPad3)
            {
                return;
            }
            else
            {
                Console.WriteLine("Please enter a valid value");
            }

            goto resetSettings;
        }

        public static void AutoswitchSettings()
        {
            Console.Clear();
            Console.WriteLine("Please press on your inventory F-key");
            Settings.Instance.InventFKey = (Keys)Console.ReadKey().Key;
            Console.WriteLine("\nYour inventory F-key is: " + Settings.Instance.InventFKey);
            Console.WriteLine();
            Settings.Instance.InventorySlots = CLI.GetInventory();
            if (Settings.Instance.InventorySlots.Count > 0)
            {
                Console.WriteLine("Successfully saved inventory data");
            }
            Console.WriteLine();
            int autoSwitches = int.MaxValue;
            int currentCounter = 0;
            retry:
            Console.WriteLine("How many autoswitches do you want?");
            try
            {
                autoSwitches = int.Parse(Console.ReadLine());
            }
            catch (Exception)
            {
                Console.WriteLine("Invalid number");
                goto retry;
            }
            while (autoSwitches != currentCounter)
            {
                Console.Clear();
                var autoSwitchItems = GetAutoSwitchItems();
                Console.WriteLine("Please press on your desired autoswitch key");
                var autoSwitchKey = (Keys)Console.ReadKey().Key;
                Console.WriteLine("\nYour autoswitch hotkey is: " + autoSwitchKey);
                if (Settings.Instance.ActiveAutoSwitchHotkeys == null)
                {
                    Settings.Instance.ActiveAutoSwitchHotkeys = new Dictionary<Keys, List<int>>();
                }
                if (Settings.Instance.ActiveAutoSwitchHotkeys.ContainsKey(autoSwitchKey))
                {
                    Settings.Instance.ActiveAutoSwitchHotkeys.Remove(autoSwitchKey);
                    Console.WriteLine("Existing hotkey was found, overwriting.");
                }
                Settings.Instance.ActiveAutoSwitchHotkeys.Add(autoSwitchKey,autoSwitchItems);
                currentCounter++;
            }
            Console.WriteLine("Successfully setup autoswitcher");
            Console.WriteLine("Press any key to return to main menu");
            Console.ReadKey();
        }

        public static void SetupSpecHelper()
        {
            Console.Clear();
            Console.WriteLine("Please press on your attack settings F-key");
            Settings.Instance.AttackFkey = (Keys) Console.ReadKey().Key;
            Console.WriteLine("\nYour attack settings F-key is: " + Settings.Instance.AttackFkey);
            Console.WriteLine();
            Console.WriteLine("Please press 'A' while hovering over the special attack bar");
            while (Console.ReadKey().Key != ConsoleKey.A)
            {
                Thread.Yield();
            }
            Settings.Instance.SpecialAttackBarPoint = Cursor.Position;
            int specWeps = int.MaxValue;
            int currentCounter = 0;
            retry:
            Console.WriteLine("\nHow many spechelpers do you want?");
            try
            {
                specWeps = int.Parse(Console.ReadLine());
            }
            catch (Exception)
            {
                Console.WriteLine("Invalid number");
                goto retry;
            }
            while (specWeps != currentCounter)
            {
                Console.Clear();
                var autoSwitchItems = GetAutoSwitchItems(false);
                Console.WriteLine("Does your current weapon require waiting for the special attack bar to show up?");
                Console.WriteLine("1 = yes | 2 = no");
                var boolval = int.Parse(Console.ReadLine());
                if (boolval == 1 || boolval == 2)
                {
                    Settings.Instance.WaitForSpecBar = boolval == 1;
                }
                else
                {
                    Console.WriteLine("Invalid value input");
                    Console.ReadKey();
                    return;
                }
                Settings.Instance.WaitForSpecBar = boolval == 1;
                Console.WriteLine("How many times do you want to spec with your current weapon?");
                var specTimes = int.Parse(Console.ReadLine());
                Console.WriteLine("Please press on your desired spechelper key");
                var spechelperKey = (Keys)Console.ReadKey().Key;
                Console.WriteLine("\nYour spechelper hotkey is: " + spechelperKey);
                Console.WriteLine("How many times");
                if (Settings.Instance.ActiveAutoSwitchHotkeys == null)
                {
                    Settings.Instance.SpecialAttackHotkeys = new Dictionary<Keys, SpecialAttackHelper>();
                }
                if (Settings.Instance.SpecialAttackHotkeys.ContainsKey(spechelperKey))
                {
                    Settings.Instance.SpecialAttackHotkeys.Remove(spechelperKey);
                    Console.WriteLine("Existing hotkey was found, overwriting.");
                }

                SpecialAttackHelper sah = new SpecialAttackHelper
                {
                    SpecTimes = specTimes,
                    InventoryId = autoSwitchItems
                };
                Settings.Instance.SpecialAttackHotkeys.Add(spechelperKey, sah);
                Console.WriteLine("Created spechelper " + currentCounter);
                currentCounter++;
            }
            Console.WriteLine("Successfully setup spechelper");
            Console.WriteLine("Press any key to return to main menu");
            Console.ReadKey();

        }

        public static void HotkeySettings()
        {
            PrintHotkeys(out var count);
            var autosCount = Settings.Instance.ActiveAutoSwitchHotkeys.Count;
            var prayerCount = Settings.Instance.ActivePrayerHotkeys.Count;
            var specialCount = Settings.Instance.SpecialAttackHotkeys.Count;

            Console.WriteLine("Enter a setting to change");
            var KeyToSwitch = (Keys)Console.ReadKey().Key;
            var status = Extensions.IsKeyADigit(KeyToSwitch);
            var convBool = int.TryParse(KeyToSwitch.ToString().Replace("D", ""), out var keyInt);

            if (!convBool || keyInt > count || !status)
            {
                Console.WriteLine("Invalid number input!");
                return;
            }
            
            Console.WriteLine("\nInput new hotkey");
            var newHotkey = (Keys)Console.ReadKey().Key;

            switch (keyInt)
            {
                case int n when (n > 0 && n <= autosCount):
                    var dictAutoswitch = Settings.Instance.ActiveAutoSwitchHotkeys.ToList()[keyInt - 1];
                    Settings.Instance.ActiveAutoSwitchHotkeys.Remove(dictAutoswitch.Key);
                    Settings.Instance.ActiveAutoSwitchHotkeys.Add(newHotkey, dictAutoswitch.Value);
                    break;
                case int n when (n > autosCount && n <= prayerCount + autosCount):
                    var index = keyInt - autosCount - 1;
                    var dictAutoPrayer = Settings.Instance.ActivePrayerHotkeys.ToList()[index];
                    Settings.Instance.ActivePrayerHotkeys.Remove(dictAutoPrayer.Key);
                    Settings.Instance.ActivePrayerHotkeys.Add(dictAutoPrayer.Key, newHotkey);
                    break;
                case int n when (n > autosCount + prayerCount && n <= prayerCount + autosCount + specialCount):
                    var specialIndex = keyInt - autosCount - prayerCount - 1;
                    var dictSpecialAtt = Settings.Instance.SpecialAttackHotkeys.ToList()[specialIndex];
                    Settings.Instance.SpecialAttackHotkeys.Remove(dictSpecialAtt.Key);
                    Settings.Instance.SpecialAttackHotkeys.Add(newHotkey, dictSpecialAtt.Value);
                    break;
                case int n when (n == autosCount + prayerCount + specialCount + 1):
                    Settings.Instance.ExitKey = newHotkey;
                    break;
                default:
                    return;

            }
        }

        private static void PrintHotkeys(out int count)
        {
            Console.Clear();
            count = 1;
            if (Settings.Instance.ActiveAutoSwitchHotkeys.Count > 0)
            {
                Console.WriteLine("Autoswitch hotkeys:");
                foreach (var aus in Settings.Instance.ActiveAutoSwitchHotkeys)
                {
                    Console.WriteLine(count + ". " + aus.Key);
                    count++;
                } 
            }

            Console.WriteLine();

            if (Settings.Instance.ActivePrayerHotkeys.Count > 0)
            {
                Console.WriteLine("Prayer hotkeys");
                foreach (var phk in Settings.Instance.ActivePrayerHotkeys)
                {
                    Console.WriteLine(count + ". " + phk.Key);
                    count++;
                }
            }

            Console.WriteLine();

            if (Settings.Instance.SpecialAttackHotkeys.Count > 0)
            {
                Console.WriteLine("Special attack hotkeys:");
                foreach (var sak in Settings.Instance.SpecialAttackHotkeys)
                {
                    Console.WriteLine(count + ". " + sak.Key);
                    count++;
                }
            }

            Console.WriteLine();

            Console.WriteLine("Application settings:");
            Console.WriteLine(count + ". Hotkey for closing OSRS-AutoSwitcher: " + Settings.Instance.ExitKey);
            Console.WriteLine(count + 1 + ". Return to main menu");
            Console.WriteLine();
        }
    }
}
