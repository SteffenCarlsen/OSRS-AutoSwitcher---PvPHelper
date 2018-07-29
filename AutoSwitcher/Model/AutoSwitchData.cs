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
        public List<InventoryItem> InventoryData { get; set; }
        public char? KeyboardHotkey { get; set; }
        public MouseButtons? MouseHotkey { get; set; }
        public bool ReturnOldPos { get; set; }
        public bool Stretched { get; set; }
        public int speed { get; set; }
        public Point FirstInventSlot { get; set; }
        public string InventKey { get; set; }

    }
}
