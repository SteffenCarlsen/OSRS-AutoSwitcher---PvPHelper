using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoSwitcher.Model
{
    public class PrayerBook
    {
        public static Dictionary<Prayer,Tuple<bool,Point>> ActivePrayerBook { get; set; }

        public enum Prayer
        {
            ThickSkin = 0,
            BurstofStrength = 1,
            ClarityofThought = 2,
            SharpEye = 3,
            MysticWill = 4,
            RockSkin = 5,
            SuperhumanStrength = 5,
            ImprovedReflexes = 6,
            RapidRestore = 7,
            RapidHeal = 8,
            ProtectItem = 9,
            HawkEye = 10,
            MysticLore = 11,
            SteelSkin = 12,
            UltimateStrength = 13,
            IncredibleReflexes = 14,
            ProtectfromMagic = 15,
            ProtectfroMissiles = 16,
            ProtectfromMelee = 17,
            EagleEye = 18,
            MysticMight = 19,
            Retribution = 20,
            Redemption = 21,
            Smite = 22,
            Preserve = 23,
            Chivalry = 24,
            Piety = 25,
            Rigour = 26,
            Augury = 28
        }

        public static Prayer GetNextPrayer(int value)
        {
            Prayer returnedEnum = (Prayer) Enum.ToObject(typeof(Prayer), value);
            return returnedEnum;
        }
    }
}
