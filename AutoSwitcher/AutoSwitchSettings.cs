using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using AutoSwitcher.Data;
using AutoSwitcher.Model;
using Gma.System.MouseKeyHook;

namespace AutoSwitcher
{
    public partial class AutoSwitchSettings : Form
    {
        private List<InventoryItem> _inventory = new List<InventoryItem>(28);

        //DataFields
        private Point _slot1;
        private char? keyChar;

        private string localizedString;

        //Datahooks
        private IKeyboardMouseEvents m_GlobalHook;
        private IKeyboardMouseEvents m_GlobalHookHotkey;
        private MouseButtons? mouseButton;
        private bool oldPos;
        private bool started;
        private bool stretched;
        private string inventKey;
        private int _speed;


        public AutoSwitchSettings()
        {
            InitializeComponent();
            FormClosing += CloseApp;
        }

        private void CloseApp(object sender, FormClosingEventArgs e)
        {
            ;
            try
            {
                Unsubscribe();
                UnsubscribeForHotkey();
            }
            catch (NullReferenceException exception)
            {
                Console.WriteLine(exception);
            }
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.ControlKey)
            {
                var p = Cursor.Position;
                _slot1 = p;
                textBox1.Text = p.ToString();
                if (stretched)
                {
                    GenerateLocationData(_slot1, 100, 70);
                }
                else
                {
                    GenerateLocationData(p, 40, 35);
                }
            }
        }

        private void GenerateLocationData(Point inventPoint,int distX,int distY)
        {
            var listNo = 0;
            var oldX = inventPoint;
            for (var i = 0; i < 7; i++)
            {
                for (var j = 0; j < 4; j++)
                {
                    _inventory[listNo].Coords = new Point(oldX.X + distX * j, oldX.Y);
                    listNo++;
                }

                oldX = new Point(oldX.X, oldX.Y + distY);
            }
        }

        private void textBox1_MouseDown(object sender, MouseEventArgs e)
        {
            textBox1.Text = "Press CTRL to get location";
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            FormBorderStyle = FormBorderStyle.FixedSingle;
            NewForm();
            UpdateDropdown();
            comboBox1.SelectedIndex = 0;
        }

        private void NewForm()
        {
            keyChar = null;
            mouseButton = null;
            started = false;
            checkBox29.Checked = false;
            //Allocates data for Inventory
            for (var i = 0; i < 28; i++)
            {
                var f = new InventoryItem();
                f.Coords = new Point(0, 0);
                f.Enabled = false;
                _inventory.Add(f);
            }
        }

        private void UpdateDropdown()
        {
            if (Directory.Exists(Environment.CurrentDirectory + "/SavedData"))
            {
                var Files = new DirectoryInfo(Environment.CurrentDirectory + "/SavedData").GetFiles();
                foreach (var data in Files) comboBox1.Items.Add(data.Name);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (started)
            {
                Unsubscribe();
                button2.Text = "Start (Hook hotkey)";
                started = false;
            }
            else
            {
                Subscribe();
                started = true;
            }
        }

        private void SubscribeForHotkey()
        {
            m_GlobalHookHotkey = Hook.GlobalEvents();

            m_GlobalHookHotkey.MouseDownExt += MouseAppPress;
            m_GlobalHookHotkey.KeyPress += HotkeyPress;
        }

        private void UnsubscribeForHotkey()
        {
            m_GlobalHookHotkey.MouseDownExt -= MouseAppPress;
            m_GlobalHookHotkey.KeyPress -= HotkeyPress;
            m_GlobalHookHotkey.Dispose();
        }

        private void HotkeyPress(object sender, KeyPressEventArgs e)
        {
            textBox2.Text = e.KeyChar.ToString();
            keyChar = e.KeyChar;

            UnsubscribeForHotkey();
        }

        private void MouseAppPress(object sender, MouseEventExtArgs e)
        {
            if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Right)
            {
                e.Handled = true;
            }
            else
            {
                textBox2.Text = e.Button.ToString();
                mouseButton = e.Button;
                UnsubscribeForHotkey();
            }
        }

        public void Subscribe()
        {
            if (keyChar != null)
            {
                m_GlobalHook = Hook.GlobalEvents();
                m_GlobalHook.KeyPress += GlobalHookKeyPress;
                button2.Text = "Stop (Unhook hotkey)";
            }
            else if (mouseButton != null)
            {
                m_GlobalHook = Hook.GlobalEvents();
                m_GlobalHook.MouseDownExt += GlobalHookMouseDownExt;
                button2.Text = "Stop (Unhook hotkey)";
            }
            else
            {
                MessageBox.Show("No hotkey selected!");
            }
        }

        public void Unsubscribe()
        {
            m_GlobalHook.MouseDownExt -= GlobalHookMouseDownExt;
            m_GlobalHook.KeyPress -= GlobalHookKeyPress;
            m_GlobalHook.Dispose();
        }

        private void GlobalHookKeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == keyChar)
                AutoSwitch();
            else
                e.Handled = true;
        }

        private void GlobalHookMouseDownExt(object sender, MouseEventExtArgs e)
        {
            if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Right) e.Handled = true;
            if (e.Button == mouseButton)
                AutoSwitch();
            else
                e.Handled = true;
        }

        private void textBox2_MouseDown(object sender, MouseEventArgs e)
        {
            textBox2.Text = "Press key to use as hotkey";
            SubscribeForHotkey();
        }

        private void AutoSwitch()
        {
            var currentPosition = Cursor.Position;
            MouseEvents.FreezeMouse();
            SendKeys.Send("{"+inventKey+"}");
            foreach (var inventSlot in _inventory)
                if (inventSlot.Enabled)
                    MouseEvents.LinearSmoothMove(inventSlot.Coords, _speed, true);
            if (oldPos) MouseEvents.LinearSmoothMove(currentPosition, _speed, false);
            MouseEvents.ThawMouse();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            var name = "";
            if (comboBox1.SelectedItem == "Default")
            {
                MessageBox.Show("Can't save preset under default name", "Error", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                var np = new NewPreset();
                np.button.Text = "Save preset";
                var res = np.ShowDialog();
                if (res == DialogResult.OK)
                {
                    name = np.name;
                    UpdateDropdown();
                }
            }
            else
            {
                name = comboBox1.SelectedItem.ToString();
            }

            var dataObject = new AutoSwitchData();
            dataObject.InventoryData = _inventory;
            dataObject.KeyboardHotkey = keyChar;
            dataObject.MouseHotkey = mouseButton;
            dataObject.ReturnOldPos = oldPos;
            dataObject.Stretched = stretched;
            dataObject.speed = _speed;
            dataObject.FirstInventSlot = _slot1;
            dataObject.InventKey = inventKey;
            var direct = Environment.CurrentDirectory;
            var fullPath = direct + "/SavedData";
            var exists = Directory.Exists(fullPath);
            if (exists || !string.IsNullOrWhiteSpace(name))
            {
                JsonObjectFileSaveLoad.WriteToJsonFile(fullPath + "/" + name + ".json", dataObject);
            }
            else if (!string.IsNullOrWhiteSpace(name))
            {
                Directory.CreateDirectory(fullPath);
                JsonObjectFileSaveLoad.WriteToJsonFile(fullPath + "/" + name + ".json", dataObject);
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            var direct = Environment.CurrentDirectory;
            var fullPath = direct + "/SavedData/" + comboBox1.SelectedItem;

            if (File.Exists(fullPath))
            {
                var data = JsonObjectFileSaveLoad.ReadFromJsonFile<AutoSwitchData>(fullPath);
                _inventory = data.InventoryData;
                _speed = data.speed;
                inventKey = data.InventKey;
                _slot1 = data.FirstInventSlot;
                stretched = data.Stretched;
                checkBox30.Checked = stretched;
                textBox1.Text = data.FirstInventSlot.ToString();
                if (data.KeyboardHotkey != null)
                    keyChar = data.KeyboardHotkey;
                else if (data.MouseHotkey != null)
                    mouseButton = data.MouseHotkey;
                UpdateUI(data);
            }
        }

        private void UpdateUI(AutoSwitchData data)
        {
            //Inventory update
            checkBox1.Checked = _inventory[0].Enabled;
            checkBox8.Checked = _inventory[1].Enabled;
            checkBox15.Checked = _inventory[2].Enabled;
            checkBox16.Checked = _inventory[3].Enabled;
            checkBox2.Checked = _inventory[4].Enabled;
            checkBox9.Checked = _inventory[5].Enabled;
            checkBox17.Checked = _inventory[6].Enabled;
            checkBox23.Checked = _inventory[7].Enabled;
            checkBox3.Checked = _inventory[8].Enabled;
            checkBox10.Checked = _inventory[9].Enabled;
            checkBox18.Checked = _inventory[10].Enabled;
            checkBox24.Checked = _inventory[11].Enabled;
            checkBox4.Checked = _inventory[12].Enabled;
            checkBox11.Checked = _inventory[13].Enabled;
            checkBox19.Checked = _inventory[14].Enabled;
            checkBox25.Checked = _inventory[15].Enabled;
            checkBox5.Checked = _inventory[16].Enabled;
            checkBox12.Checked = _inventory[17].Enabled;
            checkBox20.Checked = _inventory[18].Enabled;
            checkBox26.Checked = _inventory[19].Enabled;
            checkBox6.Checked = _inventory[20].Enabled;
            checkBox13.Checked = _inventory[21].Enabled;
            checkBox21.Checked = _inventory[22].Enabled;
            checkBox27.Checked = _inventory[23].Enabled;
            checkBox7.Checked = _inventory[24].Enabled;
            checkBox14.Checked = _inventory[25].Enabled;
            checkBox22.Checked = _inventory[26].Enabled;
            //Return to old pos
            checkBox29.Checked = data.ReturnOldPos;
            //Hotkey
            textBox2.Text = data.MouseHotkey != null ? data.MouseHotkey.ToString() : data.KeyboardHotkey.ToString();
            //Stretched
            checkBox30.Checked = data.Stretched;
            //Speed
            numericUpDown1.Value = _speed;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var np = new NewPreset();
            var res = np.ShowDialog();
            if (res == DialogResult.OK)
            {
                localizedString = np.name;
                comboBox1.Items.Add(localizedString);
                UpdateDropdown();
            }
        }

        private void checkBox29_CheckedChanged(object sender, EventArgs e)
        {
            oldPos = checkBox29.Checked;
        }

        #region Checkboxes

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            _inventory[0].Enabled = checkBox1.Checked;
        }

        private void checkBox8_CheckedChanged(object sender, EventArgs e)
        {
            _inventory[1].Enabled = checkBox8.Checked;
        }

        private void checkBox15_CheckedChanged(object sender, EventArgs e)
        {
            _inventory[2].Enabled = checkBox15.Checked;
        }

        private void checkBox16_CheckedChanged(object sender, EventArgs e)
        {
            _inventory[3].Enabled = checkBox16.Checked;
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            _inventory[4].Enabled = checkBox2.Checked;
        }

        private void checkBox9_CheckedChanged(object sender, EventArgs e)
        {
            _inventory[5].Enabled = checkBox9.Checked;
        }

        private void checkBox17_CheckedChanged(object sender, EventArgs e)
        {
            _inventory[6].Enabled = checkBox17.Checked;
        }

        private void checkBox23_CheckedChanged(object sender, EventArgs e)
        {
            _inventory[7].Enabled = checkBox23.Checked;
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            _inventory[8].Enabled = checkBox3.Checked;
        }

        private void checkBox10_CheckedChanged(object sender, EventArgs e)
        {
            _inventory[9].Enabled = checkBox10.Checked;
        }

        private void checkBox18_CheckedChanged(object sender, EventArgs e)
        {
            _inventory[10].Enabled = checkBox18.Checked;
        }

        private void checkBox24_CheckedChanged(object sender, EventArgs e)
        {
            _inventory[11].Enabled = checkBox24.Checked;
        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            _inventory[12].Enabled = checkBox4.Checked;
        }

        private void checkBox11_CheckedChanged(object sender, EventArgs e)
        {
            _inventory[13].Enabled = checkBox11.Checked;
        }

        private void checkBox19_CheckedChanged(object sender, EventArgs e)
        {
            _inventory[14].Enabled = checkBox19.Checked;
        }

        private void checkBox25_CheckedChanged(object sender, EventArgs e)
        {
            _inventory[15].Enabled = checkBox25.Checked;
        }

        private void checkBox5_CheckedChanged(object sender, EventArgs e)
        {
            _inventory[16].Enabled = checkBox5.Checked;
        }

        private void checkBox12_CheckedChanged(object sender, EventArgs e)
        {
            _inventory[17].Enabled = checkBox12.Checked;
        }

        private void checkBox20_CheckedChanged(object sender, EventArgs e)
        {
            _inventory[18].Enabled = checkBox20.Checked;
        }

        private void checkBox26_CheckedChanged(object sender, EventArgs e)
        {
            _inventory[19].Enabled = checkBox26.Checked;
        }

        private void checkBox6_CheckedChanged(object sender, EventArgs e)
        {
            _inventory[20].Enabled = checkBox6.Checked;
        }

        private void checkBox13_CheckedChanged(object sender, EventArgs e)
        {
            _inventory[21].Enabled = checkBox13.Checked;
        }

        private void checkBox21_CheckedChanged(object sender, EventArgs e)
        {
            _inventory[22].Enabled = checkBox21.Checked;
        }

        private void checkBox27_CheckedChanged(object sender, EventArgs e)
        {
            _inventory[23].Enabled = checkBox27.Checked;
        }

        private void checkBox7_CheckedChanged(object sender, EventArgs e)
        {
            _inventory[24].Enabled = checkBox7.Checked;
        }

        private void checkBox14_CheckedChanged(object sender, EventArgs e)
        {
            _inventory[25].Enabled = checkBox14.Checked;
        }

        private void checkBox22_CheckedChanged(object sender, EventArgs e)
        {
            _inventory[26].Enabled = checkBox22.Checked;
        }

        private void checkBox28_CheckedChanged(object sender, EventArgs e)
        {
            _inventory[27].Enabled = checkBox28.Checked;
        }

        #endregion

        private void checkBox30_CheckedChanged(object sender, EventArgs e)
        {
            stretched = checkBox30.Checked;
            if (stretched)
            {
                GenerateLocationData(_slot1,100,70);
            }
            else
            {
                GenerateLocationData(_slot1, 40,30);
            }
        }

        private void textBox5_KeyDown(object sender, KeyEventArgs e)
        {
            textBox5.Text = e.KeyCode.ToString();
            inventKey = e.KeyCode.ToString();
        }

        private void textBox5_MouseDown(object sender, MouseEventArgs e)
        {
            textBox5.Text = "Press hotkey for inventory";
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            _speed = (int) numericUpDown1.Value;
        }
    }
}