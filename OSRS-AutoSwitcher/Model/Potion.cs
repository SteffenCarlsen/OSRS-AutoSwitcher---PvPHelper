using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSRSAutoSwitcher.Model
{
    public class Potion
    {
        public Potion(ItemType itemType)
        {
            if (itemType != ItemType.PrayerPot || itemType != ItemType.RestorePot || itemType != ItemType.SaraBrew)
            {
                throw new ArgumentException("Potion must have a potion ItemType");
            }

            ItemType = itemType;
            Doses = 4;
        }

        public ItemType ItemType { get; set; }
        public int Doses { get; set; }
    }
}
