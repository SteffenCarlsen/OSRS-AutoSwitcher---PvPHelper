using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AutoSwitcher.Model
{
    public class AutoSwitchData
    {
        public List<InventoryItem> InventoryDataAutoSwitch { get; set; }
        public char? KeyboardHotkey { get; set; }
        public char? FoodHotkey { get; set; }
        public char? CombofoodHotkey { get; set; }
        public char? PrayerHotkey { get; set;}
        public char? BrewHotkey { get; set; }
        public char? SpecChar { get; set; }
        public bool ReturnOldPos { get; set; }
        public bool Stretched { get; set; }
        public int Speed { get; set; }
        public Point FirstInventSlot { get; set; }
        public Point FirstPrayerSlot { get; set; }
        public Point SpecBarPoint { get; set; }
        public string InventKey { get; set; }
        public PrayerBook.Prayer SpecOnePrayer { get; set; }
        public PrayerBook.Prayer SpecTwoPrayer { get; set; }


    }
}
