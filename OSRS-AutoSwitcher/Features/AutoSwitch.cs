using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using LowLevelInput.Hooks;
using OSRSAutoSwitcher.Globals;
using OSRSAutoSwitcher.Interaction;
using OSRSAutoSwitcher.Model;

namespace OSRSAutoSwitcher.Features
{
    internal class AutoSwitch
    {
        public static List<Point> GenerateInventory(Point firstInventorySlot)
        {
            List<Point> inventory = new List<Point>();
            var old = firstInventorySlot;
            for (int i = 0; i < 7; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    Point inventorySlot = new Point {X = old.X + 40 * j, Y = old.Y};
                    inventory.Add(inventorySlot);
                }
                old = new Point(old.X, old.Y + 35);
            }

            return inventory;
        }

        public static List<int> AutoswitchItems(bool autoswitcher = true)
        {
            List<int> items = new List<int>();
            Console.WriteLine();
            if (autoswitcher)
            {
                Console.WriteLine("Please type in the inventory slots you want to autoswitch");
            }
            else
            {
                Console.WriteLine("Please type in the inventory slots you want to use for special attacking");
            }
            InventoryAsciiArt();
            RetryNumber:
            Console.WriteLine("Please separate the slot number with 'space'");
            var data = Console.ReadLine();
            var split = data.Split(' ');
            foreach (var dataSplit in split)
            {
                try
                {
                    var outValue = int.Parse(dataSplit);
                    items.Add(outValue - 1);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    goto RetryNumber;
                    
                }
            }
            return items;
        }

        public static void InventoryAsciiArt()
        {
            Console.WriteLine("+-------------+");
            Console.WriteLine("| 1  2   3  4 |");
            Console.WriteLine("|             |");
            Console.WriteLine("| 5  6   7  8 |");
            Console.WriteLine("|             |");
            Console.WriteLine("| 9  10  11 12|");
            Console.WriteLine("|             |");
            Console.WriteLine("|13  14  15 16|");
            Console.WriteLine("|             |");
            Console.WriteLine("|17  18  19 20|");
            Console.WriteLine("|             |");
            Console.WriteLine("|21  22  23 24|");
            Console.WriteLine("|             |");
            Console.WriteLine("|25  26  27 28|");
            Console.WriteLine("+-------------+");
        }

        public static bool ActiveSwitches(VirtualKeyCode key, bool swappedPrayers)
        {
            bool swappedItems = false;
            foreach (var autoSwitchHotkey in Settings.Instance.ActiveAutoSwitchHotkeys)
            {
                var activeKey = LowLevelInput.Converters.KeyCodeConverter.ToVirtualKeyCode((int)autoSwitchHotkey.Key);
                if (activeKey != key) continue;
                swappedItems = true;
                // If we just swapped prayers, make a short delay
                if (swappedPrayers)
                {
                    Thread.Sleep(50);
                }

                if (Settings.Instance.ActiveAutoSwitchHotkeys != null)
                {
                    var oldPos = Cursor.Position;
                    foreach (var locations in autoSwitchHotkey.Value)
                    {
                        SendKeys.SendWait("{" + Settings.Instance.InventFKey + "}");
                        Thread.Sleep(10);
                        WindowsImports.SetCursorPos(Settings.Instance.InventorySlots[locations].X, Settings.Instance.InventorySlots[locations].Y);
                        WindowsImports.LeftMouseClick(Settings.Instance.InventorySlots[locations].X, Settings.Instance.InventorySlots[locations].Y);
                        Thread.Sleep(50);
                    }

                    if (Settings.Instance.ReturnToOldPos)
                    {
                        Mouse.LinearSmoothMove(oldPos, Settings.Instance.Speed, false);
                    }
                }
            }

            return swappedItems;
        }

    }
}
