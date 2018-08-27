using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoSwitcher
{
    public class InventoryItem
    {
        public Point Coords { get; set; }
        public bool Enabled { get; set; }
        public InventType Invent { get; set; }
        public bool Used { get; set; }
        public Doses Dosage { get; set; }

        public enum InventType
        {
            None = 0,
            NormalFood = 1,
            ComboFood = 2,
            PrayerPot = 3,
            SaraBrew = 4,
            Item = 5,
            SpecWepOne = 6,
            SpecWepTwo = 7,
            Prayer = 8
        }

        public enum Doses
        {
            None = 0,
            One = 1,
            Two = 2,
            Three = 3,
            Four = 4
        }
    }
}
