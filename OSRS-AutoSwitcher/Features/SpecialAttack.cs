using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using LowLevelInput.Hooks;
using OSRSAutoSwitcher.Interaction;
using OSRSAutoSwitcher.Model;

namespace OSRSAutoSwitcher.Features
{
    internal class SpecialAttack
    {
        public static void DoSpecialAttack(VirtualKeyCode key, bool swappedItems, bool swappedPrayers)
        {
            foreach (var hotkey in Settings.Instance.SpecialAttackHotkeys)
            {
                var activeKey = LowLevelInput.Converters.KeyCodeConverter.ToVirtualKeyCode((int)hotkey.Key);
                if (activeKey != key) continue;
                if (swappedItems || swappedPrayers)
                {
                    Thread.Sleep(50);
                }

                var oldPos = Cursor.Position;
                SendKeys.SendWait("{" + Settings.Instance.InventFKey + "}");
                foreach (var specWep in hotkey.Value.InventoryId)
                {
                    Thread.Sleep(10);
                    Mouse.LinearSmoothMove(new Point(Settings.Instance.InventorySlots[specWep].X, Settings.Instance.InventorySlots[specWep].Y), Settings.Instance.Speed, true);
                    Thread.Sleep(10);
                }

                SendKeys.SendWait("{" + Settings.Instance.AttackFkey + "}");
                Thread.Sleep(Settings.Instance.WaitForSpecBar ? 450 : 100);
                Mouse.LinearSmoothMove(oldPos, Settings.Instance.Speed, true);
                int pressedCounter = 0;
                while (pressedCounter != hotkey.Value.SpecTimes)
                {
                    Mouse.LinearSmoothMove(Settings.Instance.SpecialAttackBarPoint, Settings.Instance.Speed, true);
                    Thread.Sleep(100);
                    pressedCounter++;
                }

                Mouse.LinearSmoothMove(oldPos, Settings.Instance.Speed, true);
                if (Settings.Instance.ReturnToOldPos)
                {
                    Mouse.LinearSmoothMove(oldPos, Settings.Instance.Speed, false);
                }
            }
        }
    }
}
