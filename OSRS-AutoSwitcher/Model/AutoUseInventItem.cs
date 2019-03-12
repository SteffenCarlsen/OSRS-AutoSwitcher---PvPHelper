using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSRSAutoSwitcher.Model
{
    public class AutoUseInventItem
    {
        public Dictionary<ItemType,int> InventorySetup { get; set; }
        public Dictionary<int,Potion> InventoryPotionSnapshot { get; set; }
        public bool IsPotion(ItemType itemType)
        {
            return itemType == ItemType.PrayerPot && itemType == ItemType.RestorePot && itemType == ItemType.SaraBrew;
        }
    }

    public enum ItemType
    {
        None,
        SaraBrew,
        RestorePot,
        RegularFood,
        ComboFood,
        GuthixRest,
        Teleport,
        PrayerPot
    }

}
