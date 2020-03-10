using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using LowLevelInput.Converters;
using LowLevelInput.Hooks;
using OSRSAutoSwitcher.Interaction;
using OSRSAutoSwitcher.Model;

namespace OSRSAutoSwitcher.Features
{
    internal class PrayerSwitch
    {
        public static Dictionary<PrayerBook.Prayer, Point> GeneratePrayerBook(Point inventPoint, int distX, int distY)
        {
            var listNo = 0;
            var oldX = inventPoint;
            PrayerBook.ActivePrayerBook = new Dictionary<PrayerBook.Prayer, Point>();
            for (var i = 0; i < 6; i++)
            {
                for (var j = 0; j < 5; j++)
                {
                    PrayerBook.ActivePrayerBook.Add(PrayerBook.GetNextPrayer(listNo), new Point(oldX.X + distX * j, oldX.Y));
                    if (listNo == 29)
                    {
                        return PrayerBook.ActivePrayerBook;
                    }

                    listNo++;
                }
                oldX = new Point(oldX.X, oldX.Y + distY);
            }
            return PrayerBook.ActivePrayerBook;
        }

        public static bool ActivePrayer(VirtualKeyCode key)
        {
            bool swappedPrayers = false;
            foreach (var activePrayerHotkey in Settings.Instance.ActivePrayerHotkeys)
            {
                var activeKey = LowLevelInput.Converters.KeyCodeConverter.ToVirtualKeyCode((int)activePrayerHotkey.Value);
                if (activeKey != key) continue;
                swappedPrayers = true;
                var oldPos = Cursor.Position;
                SendKeys.SendWait("{" + Settings.Instance.PrayerFkey + "}");
                Thread.Sleep(10);
                Mouse.LinearSmoothMove(Settings.Instance.Prayers[activePrayerHotkey.Key], Settings.Instance.Speed, true);
                if (Settings.Instance.ReturnToOldPos)
                {
                    Mouse.LinearSmoothMove(oldPos, Settings.Instance.Speed, false);
                }
            }

            return swappedPrayers;
        }
    }
    public class PrayerBook
    {
        public PrayerBook()
        {
            ActivePrayerBook = new Dictionary<Prayer, Point>();
        }

        public static Dictionary<Prayer, Point> ActivePrayerBook { get; set; }

        public enum Prayer
        {
            ThickSkin = 0,
            BurstofStrength = 1,
            ClarityofThought = 2,
            SharpEye = 3,
            MysticWill = 4,
            RockSkin = 5,
            SuperhumanStrength = 6,
            ImprovedReflexes = 7,
            RapidRestore = 8,
            RapidHeal = 9,
            ProtectItem = 10,
            HawkEye = 11,
            MysticLore = 12,
            SteelSkin = 13,
            UltimateStrength = 14,
            IncredibleReflexes = 15,
            ProtectfromMagic = 16,
            ProtectfroMissiles = 17,
            ProtectfromMelee = 18,
            EagleEye = 19,
            MysticMight = 20,
            Retribution = 21,
            Redemption = 22,
            Smite = 23,
            Preserve = 24,
            Chivalry = 25,
            Piety = 26,
            Rigour = 27,
            Augury = 28
        }

        public static Prayer GetNextPrayer(int value)
        {

            Prayer returnedEnum = (Prayer)Enum.ToObject(typeof(Prayer), value);
            return returnedEnum;
        }
    }
}
