using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using AutoSwitcher.Data;
using AutoSwitcher.Model;
using Gma.System.MouseKeyHook;
using NHotkey;
using NHotkey.WindowsForms;
using Timer = System.Threading.Timer;

namespace AutoSwitcher
{
    //todo 5: Add spec wep 2 functionality + combo functionality
    //todo 6: Test functionality 
    //todo 7: Add antipattern and smooth randomized mouse movements
    //todo 8: Add shortest route to autoswitching
    //todo 8: PK TIME BBY
    //todo 9: Auto update -> https://github.com/synhershko/NAppUpdate

    public partial class AutoSwitchSettings : Form
    {
        //Threading
        private Thread _actionHandler;
        private Stack<InventoryItem.InventType> _actionOrderStack = new Stack<InventoryItem.InventType>();
        private string _attackKey;
        private char? _brewChar;
        private char? _combofoodChar;
        private Point _firstPrayer;
        private char? _foodChar;

        private string _inventKey;

        //Enumarables
        private List<InventoryItem> _inventoryAutoSwitcher = new List<InventoryItem>(28);
        private readonly List<InventoryItem> _inventoryPKHelper = new List<InventoryItem>(28);
        private readonly List<InventoryItem> _inventoryWepSpecial = new List<InventoryItem>(28);
        private char? _keyChar;
        private Keys AutoSwitchKey;
        private Keys FoodKey = Keys.None;
        private Keys ComboFoodKey = Keys.None;
        private Keys PrayerKey = Keys.None;
        private Keys SaraKey = Keys.None;
        private Keys SpecWep1Key = Keys.None;
        private Keys SpecWep2Key = Keys.None;
        private Keys MagicPrayKey = Keys.None;
        private Keys RangePrayKey = Keys.None;
        private Keys MeleePrayKey = Keys.None;
        private Keys SmitePrayKey = Keys.None;


        private string _localizedString;
        private char? _magicPray;
        private char? _meleePray;

        //Datahooks
        private IKeyboardMouseEvents _mGlobalHook;
        private IKeyboardMouseEvents _mGlobalHookHotkey;
        private MouseButtons? _mouseButton;

        private bool _oldPos;
        private int _ping;
        private PrayerBook _prayerBook;
        private char? _prayerChar;
        private string _prayerKey;
        private char? _rangePray;

        //DataFields
        private Point _slot1;
        private char? _smitePray;

        //Spec Points
        private Point _specbarPoint;
        private char? _SpecChar;

        //SpecPrayers
        private PrayerBook.Prayer _specOnePrayer;
        private bool _specPrayerOne;
        private bool _specPrayerTwo;
        private PrayerBook.Prayer _specTwoPrayer;
        private Point _specWepOne;
        private Point _SpecWepTwo;
        private int _speed;
        private bool _started;
        private bool _stretched;


        public AutoSwitchSettings()
        {
            InitializeComponent();
            FormClosing += CloseApp;
        }

        private void CloseApp(object sender, FormClosingEventArgs e)
        {
            try
            {
                Unsubscribe();
                UnsubscribeForHotkey();
                _actionHandler.Abort();
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
                if (_stretched)
                {
                    GenerateLocationData(_slot1, 100, 70);
                }
                else
                {
                    GenerateLocationData(p, 40, 35);
                }
            }
        }

        private void GenerateLocationData(Point inventPoint, int distX, int distY)
        {
            var listNo = 0;
            var oldX = inventPoint;
            for (var i = 0; i < 7; i++)
            {
                for (var j = 0; j < 4; j++)
                {
                    _inventoryAutoSwitcher[listNo].Coords = new Point(oldX.X + distX * j, oldX.Y);
                    listNo++;
                }

                oldX = new Point(oldX.X, oldX.Y + distY);
            }
        }

        private void GeneratePrayerBook(Point inventPoint, int distX, int distY)
        {
            var listNo = 0;
            var oldX = inventPoint;
            PrayerBook.ActivePrayerBook = new Dictionary<PrayerBook.Prayer, Tuple<bool, Point>>();
            for (var i = 0; i < 6; i++)
            {
                for (var j = 0; j < 5; j++)
                {
                    PrayerBook.ActivePrayerBook.Add(PrayerBook.GetNextPrayer(listNo),
                        new Tuple<bool, Point>(false, new Point(oldX.X + distX * j, oldX.Y)));
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
            Size = new Size(504, 395);
            _actionHandler = new Thread(ActionAsync);
            _actionHandler.IsBackground = true;
            _actionHandler.Start();
            NewForm();
            UpdateDropdown();
            comboBox1.SelectedIndex = 0;
        }

        private void NewForm()
        {
            _keyChar = null;
            _mouseButton = null;
            _foodChar = null;
            _brewChar = null;
            _combofoodChar = null;
            _prayerChar = null;
            _SpecChar = null;
            _started = false;
            _inventKey = null;
            _speed = 1;
            _slot1 = new Point();

            checkBox29.Checked = false;
            _prayerBook = new PrayerBook();
            tabControl1.Refresh();
            Refresh();
            _inventoryAutoSwitcher = new List<InventoryItem>();
            //Allocates data for Inventory
            for (var i = 0; i < 28; i++)
            {
                var f = new InventoryItem();
                f.Coords = new Point(0, 0);
                f.Enabled = false;
                f.Invent = InventoryItem.InventType.None;
                _inventoryAutoSwitcher.Add(f);
                _inventoryPKHelper.Add(f);
                _inventoryWepSpecial.Add(f);
            }
        }

        private async void ActionAsync()
        {
            try
            {
                while (true)
                {
                    if (_actionOrderStack.Count == 0)
                    {
                        Thread.Sleep(50);
                    }
                    else
                    {
                        var val = _actionOrderStack.Pop();
                        switch (val)
                        {
                            case InventoryItem.InventType.ComboFood:
                                await AutoEat(val);
                                break;
                            case InventoryItem.InventType.SaraBrew:
                                await AutoEat(val);
                                break;
                            case InventoryItem.InventType.NormalFood:
                                await AutoEat(val);
                                break;
                            case InventoryItem.InventType.PrayerPot:
                                await AutoEat(val);
                                break;
                            case InventoryItem.InventType.Item:
                                await AutoSwitch();
                                break;
                            case InventoryItem.InventType.SpecWepOne:
                                await SpecialAttack();
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
            catch (InvalidOperationException exception)
            {
                Console.WriteLine("ERROR: " + exception.Message);
            }
        }

        private void UpdateDropdown()
        {
            var index = 0;
            if (Directory.Exists(Environment.CurrentDirectory + "/SavedData"))
            {
                var Files = new DirectoryInfo(Environment.CurrentDirectory + "/SavedData").GetFiles();
                foreach (var data in Files)
                {
                    index = comboBox1.Items.Add(data.Name);
                }
            }

            comboBox1.SelectedIndex = index;
            foreach (var name in Enum.GetNames(typeof(PrayerBook.Prayer)))
            {
                comboBox2.Items.Add(name);
                comboBox3.Items.Add(name);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (_started)
            {
                Unsubscribe();
                button2.Text = "Start (Hook hotkey)";
                _actionOrderStack.Clear();
                _started = false;
            }
            else
            {
                Subscribe();
                _actionOrderStack = new Stack<InventoryItem.InventType>();
                _started = true;

            }
        }

        private void HookHotkeys()
        {
            if (SmitePrayKey != Keys.None)
            {
                HotkeyManager.Current.AddOrReplace("SmiteOn", SmitePrayKey, SmiteOn);
            }
            if (AutoSwitchKey != Keys.None)
            {
                HotkeyManager.Current.AddOrReplace("AutoSwitch", AutoSwitchKey, SwitchArmor);
            }
            if (FoodKey != Keys.None)
            {
                HotkeyManager.Current.AddOrReplace("Normalfood", FoodKey, NormalFood);
            }
            if (ComboFoodKey != Keys.None)
            {
                HotkeyManager.Current.AddOrReplace("ComboFood", ComboFoodKey, ComboFood);
            }

            if (PrayerKey != Keys.None)
            {
                HotkeyManager.Current.AddOrReplace("Prayer", PrayerKey, Prayer);
            }

            if (SpecWep1Key != Keys.None)
            {
                HotkeyManager.Current.AddOrReplace("SpecWepOne", SpecWep1Key, SpecialAttackWep1);

            }
            if (SaraKey != Keys.None)
            {
                HotkeyManager.Current.AddOrReplace("Sara", SaraKey, SaradominBrew);

            }

            if (MagicPrayKey != Keys.None)
            {
                HotkeyManager.Current.AddOrReplace("Magic", MagicPrayKey, MageOn);

            }

            if (MeleePrayKey != Keys.None)
            {
                HotkeyManager.Current.AddOrReplace("Melee", MeleePrayKey, MeleeOn);

            }

            if (RangePrayKey != Keys.None)
            {
                HotkeyManager.Current.AddOrReplace("Range", RangePrayKey, RangeOn);

            }
        }

        private void SaradominBrew(object sender, HotkeyEventArgs e)
        {
            _actionOrderStack.Push(InventoryItem.InventType.SaraBrew);

        }

        private void SpecialAttackWep1(object sender, HotkeyEventArgs e)
        {
            _actionOrderStack.Push(InventoryItem.InventType.SpecWepOne);
        }

        private void Prayer(object sender, HotkeyEventArgs e)
        {
            _actionOrderStack.Push(InventoryItem.InventType.PrayerPot);
        }

        private void ComboFood(object sender, HotkeyEventArgs e)
        {
            _actionOrderStack.Push(InventoryItem.InventType.ComboFood);
        }

        private void NormalFood(object sender, HotkeyEventArgs e)
        {
            _actionOrderStack.Push(InventoryItem.InventType.NormalFood);

        }

        private void SwitchArmor(object sender, HotkeyEventArgs e)
        {
            _actionOrderStack.Push(InventoryItem.InventType.Item);
        }


        private void UnHookHotkeys()
        {
            HotkeyManager.Current.Remove("SmiteOn");
            HotkeyManager.Current.Remove("AutoSwitch");
            HotkeyManager.Current.Remove("Prayer");
            HotkeyManager.Current.Remove("Normalfood");
            HotkeyManager.Current.Remove("ComboFood");
            HotkeyManager.Current.Remove("SpecWepOne");
            HotkeyManager.Current.Remove("Range");
            HotkeyManager.Current.Remove("Melee");
            HotkeyManager.Current.Remove("Magic");
            HotkeyManager.Current.Remove("Sara");
            UnsubscribeForHotkey();
        }

        private void SubscribeForHotkey(int callerId)
        {
            _mGlobalHookHotkey = Hook.GlobalEvents();
            if (callerId == 1)
            {
                _mGlobalHookHotkey.KeyPress += HotkeyPressOne;
            }

            if (callerId == 2)
            {
                _mGlobalHookHotkey.KeyPress += HotkeyPressTwo;
            }

            if (callerId == 3)
            {
                _mGlobalHookHotkey.KeyPress += HotkeyPressThree;
            }

            if (callerId == 4)
            {
                _mGlobalHookHotkey.KeyPress += HotkeyPressFour;
            }

            if (callerId == 5)
            {
                _mGlobalHookHotkey.KeyPress += HotkeyPressFive;
            }

            if (callerId == 6)
            {
                _mGlobalHookHotkey.KeyPress += HotkeyPressSix;
            }

            if (callerId == 7)
            {
                _mGlobalHookHotkey.KeyPress += HotkeyPressSeven;
            }

            if (callerId == 8)
            {
                _mGlobalHookHotkey.KeyPress += HotkeyPressEigth;
            }

            if (callerId == 9)
            {
                _mGlobalHookHotkey.KeyPress += HotkeyPressNine;
            }

            if (callerId == 10)
            {
                _mGlobalHookHotkey.KeyPress += HotkeyPressTen;
            }
        }

        private void HotkeyPressTen(object sender, KeyPressEventArgs e)
        {
            _smitePray = e.KeyChar;
            SmitePrayKey = (Keys)char.ToUpper((char)e.KeyChar);

            UnsubscribeForHotkey();
        }

        private void HotkeyPressNine(object sender, KeyPressEventArgs e)
        {
            _meleePray = e.KeyChar;
            MeleePrayKey = (Keys)char.ToUpper((char)e.KeyChar);

            UnsubscribeForHotkey();
        }

        private void HotkeyPressEigth(object sender, KeyPressEventArgs e)
        {
            _rangePray = e.KeyChar;
            RangePrayKey = (Keys)char.ToUpper((char)e.KeyChar);

            UnsubscribeForHotkey();
        }

        private void HotkeyPressSeven(object sender, KeyPressEventArgs e)
        {
            _magicPray = e.KeyChar;
            MagicPrayKey = (Keys)char.ToUpper((char)e.KeyChar);

            UnsubscribeForHotkey();
        }

        private void HotkeyPressSix(object sender, KeyPressEventArgs e)
        {
            _SpecChar = e.KeyChar;
            SpecWep1Key = (Keys)char.ToUpper((char)e.KeyChar);

            UnsubscribeForHotkey();
        }

        private void HotkeyPressFive(object sender, KeyPressEventArgs e)
        {
            _brewChar = e.KeyChar;
            SaraKey = (Keys)char.ToUpper((char)e.KeyChar);

            UnsubscribeForHotkey();
        }

        private void HotkeyPressFour(object sender, KeyPressEventArgs e)
        {
            _prayerChar = e.KeyChar;
            PrayerKey = (Keys)char.ToUpper((char)e.KeyChar);

            UnsubscribeForHotkey();
        }

        private void HotkeyPressThree(object sender, KeyPressEventArgs e)
        {
            _combofoodChar = e.KeyChar;
            ComboFoodKey = (Keys)char.ToUpper((char)e.KeyChar);
            UnsubscribeForHotkey();
        }

        private void HotkeyPressTwo(object sender, KeyPressEventArgs e)
        {
            _foodChar = e.KeyChar;
            FoodKey = (Keys)char.ToUpper((char)_foodChar);
            UnsubscribeForHotkey();
        }

        private void UnsubscribeForHotkey()
        {
            _mGlobalHookHotkey.KeyPress -= HotkeyPressOne;
            _mGlobalHookHotkey.KeyPress -= HotkeyPressTwo;
            _mGlobalHookHotkey.KeyPress -= HotkeyPressThree;
            _mGlobalHookHotkey.KeyPress -= HotkeyPressFour;
            _mGlobalHookHotkey.KeyPress -= HotkeyPressFive;
            _mGlobalHookHotkey.KeyPress -= HotkeyPressSix;
            _mGlobalHookHotkey.KeyPress -= HotkeyPressSeven;
            _mGlobalHookHotkey.KeyPress -= HotkeyPressEigth;
            _mGlobalHookHotkey.KeyPress -= HotkeyPressNine;
            _mGlobalHookHotkey.KeyPress -= HotkeyPressTen;
            _mGlobalHookHotkey.Dispose();
        }

        private void HotkeyPressOne(object sender, KeyPressEventArgs e)
        {
            textBox2.Text = e.KeyChar.ToString();
            _keyChar = e.KeyChar;
            AutoSwitchKey = (Keys)char.ToUpper((char)_keyChar);
            UnsubscribeForHotkey();
        }

        public void Subscribe()
        {
            if (_keyChar != null || _foodChar != null || _prayerChar != null || _combofoodChar != null || _brewChar != null)
            {
                HookHotkeys();
                button2.Text = "Stop (Unhook hotkey)";
            }
            else
            {
                MessageBox.Show("No hotkey selected!");
            }
        }

        public void Unsubscribe()
        {
            UnHookHotkeys();
        }
       
        private void textBox2_MouseDown(object sender, MouseEventArgs e)
        {
            textBox2.Text = "Press key to use as hotkey";
            SubscribeForHotkey(1);
        }

        private void UsePrayer(PrayerBook.Prayer prayer)
        {
            foreach (var tuple in PrayerBook.ActivePrayerBook)
            {
                if (tuple.Key == prayer)
                {
                    var currentPosition = Cursor.Position;
                    MouseEvents.FreezeMouse();
                    SendKeys.SendWait("{" + _prayerKey + "}");
                    MouseEvents.LinearSmoothMove(tuple.Value.Item2, _speed, true);
                    Console.WriteLine("We got here");
                    if (_oldPos)
                    {
                        MouseEvents.LinearSmoothMove(currentPosition, _speed, false);
                    }
                    break;
                }
            }
            MouseEvents.ThawMouse();
        }

        private async Task<bool> AutoSwitch()
        {
            var currentPosition = Cursor.Position;
            MouseEvents.FreezeMouse();
            SendKeys.SendWait("{" + _inventKey + "}");
            foreach (var inventSlot in _inventoryAutoSwitcher)
            {
                if (inventSlot.Invent == InventoryItem.InventType.Item)
                {
                    MouseEvents.LinearSmoothMove(inventSlot.Coords, _speed, true);
                }
            }

            if (_oldPos)
            {
                MouseEvents.LinearSmoothMove(currentPosition, _speed, false);
            }

            MouseEvents.ThawMouse();
            return true;
        }

        private async Task<bool> SpecialAttack()
        {
            var currentPosition = Cursor.Position;
            var lastPos = new Point();
            MouseEvents.FreezeMouse();
            //Goto inventory
            SendKeys.SendWait("{" + _inventKey + "}");
            //Find spec weapon 1
            foreach (var inventoryItem in _inventoryAutoSwitcher)
            {
                if (inventoryItem.Invent == InventoryItem.InventType.SpecWepOne)
                {
                    MouseEvents.LinearSmoothMove(inventoryItem.Coords, _speed, true);
                    lastPos = inventoryItem.Coords;
                }
            }
            

            //Check for prayers
            if (_specPrayerOne)
            {
                //Get prayer
                //_specOnePrayer
                var outval = PrayerBook.ActivePrayerBook.TryGetValue(_specOnePrayer, out var res);
                if (outval)
                {
                    UsePrayer(_specOnePrayer);
                   // SendKeys.SendWait("{" + _prayerKey + "}");
                    //MouseEvents.LinearSmoothMove(res.Item2, _speed, true);
                }
            }

            //Goto Special attack tab
            SendKeys.SendWait("{" + _attackKey + "}");
            //Press enemy
            MouseEvents.LinearSmoothMove(currentPosition, _speed, true);
            //Goto special special attack bar
            MouseEvents.LinearSmoothMove(_specbarPoint, _speed, false);
            //Press number of times
            var value = (int) numericUpDown1.Value;
            var counter = 0;
            while (counter != value)
            {
                MouseEvents.LeftMouseClick(lastPos.X, lastPos.Y);
                Thread.Sleep(50);
                counter++;
            }

            MouseEvents.ThawMouse();
            ;
            return true;
        }

        private async Task<bool> AutoEat(InventoryItem.InventType eatType)
        {
            InventoryItem localInventoryItem;
            if (eatType == InventoryItem.InventType.SaraBrew || eatType == InventoryItem.InventType.PrayerPot)
            {
                localInventoryItem = _inventoryAutoSwitcher.Select(x => x)
                    .First(x => x.Invent == eatType && x.Used == false && x.Dosage > 0);
                localInventoryItem.Dosage--;
                if (localInventoryItem.Dosage == 0)
                {
                    localInventoryItem.Used = true;
                }
            }
            else
            {
                localInventoryItem = _inventoryAutoSwitcher.Select(x => x)
                    .First(x => x.Invent == eatType && x.Used == false);
                localInventoryItem.Used = true;
            }

            var currentPosition = Cursor.Position;
            MouseEvents.FreezeMouse();
            SendKeys.SendWait("{" + _inventKey + "}");
            MouseEvents.LinearSmoothMove(localInventoryItem.Coords, _speed, true);
            if (_oldPos)
            {
                MouseEvents.LinearSmoothMove(currentPosition, _speed, false);
            }

            MouseEvents.ThawMouse();
            return true;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            SaveData();
        }

        private void SaveData()
        {
            var name = (string) comboBox1.SelectedItem;
            if (name == "Default")
            {
                MessageBox.Show("Can't save preset under default name", "Error", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                var np = new NewPreset();
                np.button.Text = "Save preset";
                var res = np.ShowDialog();
                if (res == DialogResult.OK)
                {
                    name = np.name;
                    comboBox1.Items.Add(name);
                    var index = comboBox1.Items.IndexOf(name);
                    comboBox1.SelectedIndex = index;
                    SaveData();
                }
            }
            else
            {
                name = comboBox1.SelectedItem.ToString();
            }

            var dataObject = new AutoSwitchData();
            dataObject.InventoryDataAutoSwitch = _inventoryAutoSwitcher;
            //Hotkeys
            dataObject.KeyboardHotkey = _keyChar;
            dataObject.InventKey = _inventKey;
            dataObject.BrewHotkey = _brewChar;
            dataObject.CombofoodHotkey = _combofoodChar;
            dataObject.FoodHotkey = _foodChar;
            dataObject.PrayerHotkey = _prayerChar;
            dataObject.SpecChar = _SpecChar;
            //General settings
            dataObject.ReturnOldPos = _oldPos;
            dataObject.Stretched = _stretched;
            dataObject.Speed = _speed;
            //Point objects
            dataObject.FirstInventSlot = _slot1;
            dataObject.FirstPrayerSlot = _firstPrayer;
            dataObject.SpecBarPoint = _specbarPoint;
            //Prayers
            dataObject.SpecOnePrayer = _specOnePrayer;
            dataObject.SpecTwoPrayer = _specTwoPrayer;

            var direct = Environment.CurrentDirectory;
            var fullPath = direct + "/SavedData";
            var exists = Directory.Exists(fullPath);
            if (!exists)
            {
                Directory.CreateDirectory(fullPath);
            }

            if (exists || !string.IsNullOrWhiteSpace(name))
            {
                JsonObjectFileSaveLoad.WriteToJsonFile(fullPath + "/" + name, dataObject);
            }
            else if (!string.IsNullOrWhiteSpace(name))
            {
                Directory.CreateDirectory(fullPath);
                JsonObjectFileSaveLoad.WriteToJsonFile(fullPath + "/" + name, dataObject);
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            var direct = Environment.CurrentDirectory;
            var fullPath = direct + "/SavedData/" + comboBox1.SelectedItem;
            if (File.Exists(fullPath))
            {
                var data = JsonObjectFileSaveLoad.ReadFromJsonFile<AutoSwitchData>(fullPath);
                _inventoryAutoSwitcher = data.InventoryDataAutoSwitch;
                _speed = data.Speed;
                _inventKey = data.InventKey;
                _slot1 = data.FirstInventSlot;
                _stretched = data.Stretched;
                _brewChar = data.BrewHotkey;
                _prayerChar = data.PrayerHotkey;
                _foodChar = data.FoodHotkey;
                _combofoodChar = data.CombofoodHotkey;
                checkBox30.Checked = _stretched;
                _keyChar = data.KeyboardHotkey;
                _firstPrayer = data.FirstPrayerSlot;
                _specOnePrayer = data.SpecOnePrayer;
                _specTwoPrayer = data.SpecTwoPrayer;
                _SpecChar = data.SpecChar;
                _oldPos = data.ReturnOldPos;
                _specbarPoint = data.SpecBarPoint;
                UpdateUI(data);
            }
            else
            {
                NewForm();
                var dataObject = new AutoSwitchData();
                dataObject.InventoryDataAutoSwitch = _inventoryAutoSwitcher;
                //Hotkeys
                dataObject.KeyboardHotkey = _keyChar;
                dataObject.InventKey = _inventKey;
                dataObject.BrewHotkey = _brewChar;
                dataObject.CombofoodHotkey = _combofoodChar;
                dataObject.FoodHotkey = _foodChar;
                dataObject.PrayerHotkey = _prayerChar;
                dataObject.SpecChar = _SpecChar;
                //General settings
                dataObject.ReturnOldPos = _oldPos;
                dataObject.Stretched = _stretched;
                dataObject.Speed = _speed;
                //Point objects
                dataObject.FirstInventSlot = _slot1;
                dataObject.FirstPrayerSlot = _firstPrayer;
                //Prayers
                dataObject.SpecOnePrayer = _specOnePrayer;
                dataObject.SpecTwoPrayer = _specTwoPrayer;
                UpdateUI(dataObject);
                UpdatePKHelperUI();
            }
        }

        private void UpdateUI(AutoSwitchData data)
        {
            //Inventory update
            checkBox1.Checked = _inventoryAutoSwitcher[0].Enabled;
            checkBox8.Checked = _inventoryAutoSwitcher[1].Enabled;
            checkBox15.Checked = _inventoryAutoSwitcher[2].Enabled;
            checkBox16.Checked = _inventoryAutoSwitcher[3].Enabled;
            checkBox2.Checked = _inventoryAutoSwitcher[4].Enabled;
            checkBox9.Checked = _inventoryAutoSwitcher[5].Enabled;
            checkBox17.Checked = _inventoryAutoSwitcher[6].Enabled;
            checkBox23.Checked = _inventoryAutoSwitcher[7].Enabled;
            checkBox3.Checked = _inventoryAutoSwitcher[8].Enabled;
            checkBox10.Checked = _inventoryAutoSwitcher[9].Enabled;
            checkBox18.Checked = _inventoryAutoSwitcher[10].Enabled;
            checkBox24.Checked = _inventoryAutoSwitcher[11].Enabled;
            checkBox4.Checked = _inventoryAutoSwitcher[12].Enabled;
            checkBox11.Checked = _inventoryAutoSwitcher[13].Enabled;
            checkBox19.Checked = _inventoryAutoSwitcher[14].Enabled;
            checkBox25.Checked = _inventoryAutoSwitcher[15].Enabled;
            checkBox5.Checked = _inventoryAutoSwitcher[16].Enabled;
            checkBox12.Checked = _inventoryAutoSwitcher[17].Enabled;
            checkBox20.Checked = _inventoryAutoSwitcher[18].Enabled;
            checkBox26.Checked = _inventoryAutoSwitcher[19].Enabled;
            checkBox6.Checked = _inventoryAutoSwitcher[20].Enabled;
            checkBox13.Checked = _inventoryAutoSwitcher[21].Enabled;
            checkBox21.Checked = _inventoryAutoSwitcher[22].Enabled;
            checkBox27.Checked = _inventoryAutoSwitcher[23].Enabled;
            checkBox7.Checked = _inventoryAutoSwitcher[24].Enabled;
            checkBox14.Checked = _inventoryAutoSwitcher[25].Enabled;
            checkBox22.Checked = _inventoryAutoSwitcher[26].Enabled;
            //Return to old pos
            checkBox29.Checked = data.ReturnOldPos;
            //Hotkey
            textBox2.Text = data.KeyboardHotkey.ToString();
            //Stretched
            checkBox30.Checked = data.Stretched;
            //Speed
            numericUpDown1.Value = _speed;
            var index = 1;
            foreach (var slot in _inventoryAutoSwitcher)
            {
                if (slot.Invent == InventoryItem.InventType.ComboFood)
                {
                    textBox4.Text += index + " ";
                }
                else if (slot.Invent == InventoryItem.InventType.NormalFood)
                {
                    textBox3.Text += index + " ";
                }
                else if (slot.Invent == InventoryItem.InventType.PrayerPot)
                {
                    textBox11.Text += index + " ";
                }
                else if (slot.Invent == InventoryItem.InventType.SaraBrew)
                {
                    textBox10.Text += index + " ";
                }
                else if (slot.Invent == InventoryItem.InventType.SpecWepOne)
                {
                    textBox12.Text = index.ToString();
                }
                else if (slot.Invent == InventoryItem.InventType.SpecWepTwo)
                {
                    textBox14.Text = index.ToString();
                }

                index++;
            }

            textBox1.Text = data.FirstInventSlot.ToString();
            textBox6.Text = data.FoodHotkey.ToString();
            textBox7.Text = data.CombofoodHotkey.ToString();
            textBox9.Text = data.PrayerHotkey.ToString();
            textBox8.Text = data.BrewHotkey.ToString();
            textBox5.Text = data.InventKey;
            textBox15.Text = data.SpecBarPoint.ToString();
            textBox13.Text = data.SpecChar.ToString();
            textBox16.Text = data.PrayerHotkey.ToString();
            textBox18.Text = data.FirstPrayerSlot.ToString();
            var prayIndex = comboBox2.Items.IndexOf(data.SpecOnePrayer);
            comboBox2.SelectedIndex = prayIndex;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var np = new NewPreset();
            var res = np.ShowDialog();
            if (res == DialogResult.OK)
            {
                _localizedString = np.name;
                comboBox1.Items.Add(_localizedString);
                UpdateDropdown();
            }
        }

        private void checkBox30_CheckedChanged(object sender, EventArgs e)
        {
            _stretched = checkBox30.Checked;
            if (_stretched)
            {
                GenerateLocationData(_slot1, 100, 70);
            }
            else
            {
                GenerateLocationData(_slot1, 40, 30);
            }
        }

        private void textBox5_KeyDown(object sender, KeyEventArgs e)
        {
            textBox5.Text = e.KeyCode.ToString();
            _inventKey = e.KeyCode.ToString();
        }

        private void textBox5_MouseDown(object sender, MouseEventArgs e)
        {
            textBox5.Text = "Press hotkey for inventory";
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            _speed = (int) numericUpDown1.Value;
        }


        private void button4_Click(object sender, EventArgs e)
        {
            var currentIndex = 0;
            foreach (var inv in _inventoryAutoSwitcher)
            {
                if (inv.Enabled)
                {
                    _inventoryAutoSwitcher[currentIndex].Invent = InventoryItem.InventType.NormalFood;
                }
                else if (!inv.Enabled &&
                         _inventoryAutoSwitcher[currentIndex].Invent == InventoryItem.InventType.NormalFood)
                {
                    _inventoryAutoSwitcher[currentIndex].Invent = InventoryItem.InventType.None;
                }

                currentIndex++;
            }

            UpdatePKHelperUI();
            UpdateSpecHelperUI();
            UpdateAutoswitcherUI();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            var currentIndex = 0;
            foreach (var inv in _inventoryAutoSwitcher)
            {
                if (inv.Enabled)
                {
                    _inventoryAutoSwitcher[currentIndex].Invent = InventoryItem.InventType.ComboFood;
                }
                else if (!inv.Enabled &&
                         _inventoryAutoSwitcher[currentIndex].Invent == InventoryItem.InventType.ComboFood)
                {
                    _inventoryAutoSwitcher[currentIndex].Invent = InventoryItem.InventType.None;
                }

                currentIndex++;
            }

            UpdatePKHelperUI();
            UpdateSpecHelperUI();
            UpdateAutoswitcherUI();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            ResetEatCheckboxes();
        }

        private void ResetEatCheckboxes()
        {
            checkBox31.Checked = false;
            checkBox32.Checked = false;
            checkBox33.Checked = false;
            checkBox34.Checked = false;
            checkBox35.Checked = false;
            checkBox36.Checked = false;
            checkBox37.Checked = false;
            checkBox38.Checked = false;
            checkBox39.Checked = false;
            checkBox40.Checked = false;
            checkBox41.Checked = false;
            checkBox42.Checked = false;
            checkBox43.Checked = false;
            checkBox44.Checked = false;
            checkBox45.Checked = false;
            checkBox46.Checked = false;
            checkBox47.Checked = false;
            checkBox48.Checked = false;
            checkBox49.Checked = false;
            checkBox50.Checked = false;
            checkBox51.Checked = false;
            checkBox52.Checked = false;
            checkBox53.Checked = false;
            checkBox54.Checked = false;
            checkBox55.Checked = false;
            checkBox56.Checked = false;
            checkBox57.Checked = false;
            checkBox58.Checked = false;
        }
        private void ResetSwitchCheckboxes()
        {
            checkBox1.Checked = false;
            checkBox2.Checked = false;
            checkBox3.Checked = false;
            checkBox4.Checked = false;
            checkBox5.Checked = false;
            checkBox6.Checked = false;
            checkBox7.Checked = false;
            checkBox8.Checked = false;
            checkBox9.Checked = false;
            checkBox10.Checked = false;
            checkBox11.Checked = false;
            checkBox12.Checked = false;
            checkBox13.Checked = false;
            checkBox14.Checked = false;
            checkBox15.Checked = false;
            checkBox16.Checked = false;
            checkBox17.Checked = false;
            checkBox18.Checked = false;
            checkBox19.Checked = false;
            checkBox20.Checked = false;
            checkBox21.Checked = false;
            checkBox22.Checked = false;
            checkBox23.Checked = false;
            checkBox24.Checked = false;
            checkBox25.Checked = false;
            checkBox26.Checked = false;
            checkBox27.Checked = false;
            checkBox28.Checked = false;
        }
        private void ResetSpecialCheckboxes()
        {
            checkBox59.Checked = false;
            checkBox60.Checked = false;
            checkBox61.Checked = false;
            checkBox62.Checked = false;
            checkBox87.Checked = false;
            checkBox64.Checked = false;
            checkBox65.Checked = false;
            checkBox66.Checked = false;
            checkBox67.Checked = false;
            checkBox68.Checked = false;
            checkBox69.Checked = false;
            checkBox70.Checked = false;
            checkBox71.Checked = false;
            checkBox72.Checked = false;
            checkBox73.Checked = false;
            checkBox74.Checked = false;
            checkBox75.Checked = false;
            checkBox76.Checked = false;
            checkBox77.Checked = false;
            checkBox78.Checked = false;
            checkBox79.Checked = false;
            checkBox80.Checked = false;
            checkBox81.Checked = false;
            checkBox82.Checked = false;
            checkBox83.Checked = false;
            checkBox84.Checked = false;
            checkBox85.Checked = false;
            checkBox86.Checked = false;
        }

        private void button8_Click(object sender, EventArgs e)
        {
            var currentIndex = 0;
            foreach (var inv in _inventoryAutoSwitcher)
            {
                if (inv.Enabled)
                {
                    _inventoryAutoSwitcher[currentIndex].Invent = InventoryItem.InventType.PrayerPot;
                    _inventoryAutoSwitcher[currentIndex].Dosage = InventoryItem.Doses.Four;
                }
                else if (!inv.Enabled &&
                         _inventoryAutoSwitcher[currentIndex].Invent == InventoryItem.InventType.PrayerPot)
                {
                    _inventoryAutoSwitcher[currentIndex].Invent = InventoryItem.InventType.None;
                    _inventoryAutoSwitcher[currentIndex].Dosage = InventoryItem.Doses.None;
                }

                currentIndex++;
            }

            UpdatePKHelperUI();
            UpdateSpecHelperUI();
            UpdateAutoswitcherUI();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            var currentIndex = 0;
            foreach (var inv in _inventoryAutoSwitcher)
            {
                if (inv.Enabled)
                {
                    _inventoryAutoSwitcher[currentIndex].Invent = InventoryItem.InventType.SaraBrew;
                    _inventoryAutoSwitcher[currentIndex].Dosage = InventoryItem.Doses.Four;
                }
                else if (!inv.Enabled &&
                         _inventoryAutoSwitcher[currentIndex].Invent == InventoryItem.InventType.SaraBrew)
                {
                    _inventoryAutoSwitcher[currentIndex].Invent = InventoryItem.InventType.None;
                    _inventoryAutoSwitcher[currentIndex].Dosage = InventoryItem.Doses.None;
                }

                currentIndex++;
            }

            UpdatePKHelperUI();
            UpdateSpecHelperUI();
            UpdateAutoswitcherUI();
        }

        private void UpdatePKHelperUI()
        {
            var currentIndex = 0;
            textBox10.Text = "";
            textBox11.Text = "";
            textBox4.Text = "";
            textBox3.Text = "";
            foreach (var inv in _inventoryAutoSwitcher)
            {
                var invslot = currentIndex + 1;
                switch (inv.Invent)
                {
                    case InventoryItem.InventType.ComboFood:
                        textBox4.Text += invslot + " ";
                        break;
                    case InventoryItem.InventType.SaraBrew:
                        textBox10.Text += invslot + " ";
                        break;
                    case InventoryItem.InventType.NormalFood:
                        textBox3.Text += invslot + " ";
                        break;
                    case InventoryItem.InventType.PrayerPot:
                        textBox11.Text += invslot + " ";
                        break;
                    default:
                        break;
                }

                currentIndex++;
            }
        }

        private void UpdateSpecHelperUI()
        {
            var currentIndex = 0;
            textBox12.Text = "";
            textBox14.Text = "";
            foreach (var inv in _inventoryAutoSwitcher)
            {
                var invslot = currentIndex + 1;
                switch (inv.Invent)
                {
                    case InventoryItem.InventType.SpecWepOne:
                        textBox12.Text += invslot + " ";
                        break;
                    case InventoryItem.InventType.SpecWepTwo:
                        textBox14.Text += invslot + " ";
                        break;
                    default:
                        break;
                }

                currentIndex++;
            }
        }

        private void UpdateAutoswitcherUI()
        {
            textBox19.Text = "";
            var currentIndex = 0;
            foreach (var inv in _inventoryAutoSwitcher)
            {
                var invslot = currentIndex + 1;
                switch (inv.Invent)
                {
                    case InventoryItem.InventType.Item:
                        textBox19.Text += invslot + " ";
                        break;
                    default:
                        break;
                }

                currentIndex++;
            }

            //ResetEatCheckboxes();
        }

        private void button9_Click(object sender, EventArgs e)
        {
            foreach (var inv in _inventoryAutoSwitcher)
            {
                if (inv.Invent == InventoryItem.InventType.PrayerPot || inv.Invent == InventoryItem.InventType.SaraBrew)
                {
                    inv.Dosage = InventoryItem.Doses.Four;
                }

                inv.Used = false;
            }
        }

        private void textBox14_MouseDown(object sender, MouseEventArgs e)
        {
            textBox14.Text = "Press CTRL to get location";
        }

        private void textBox15_MouseDown(object sender, MouseEventArgs e)
        {
            textBox15.Text = "Press CTRL to get location";
        }

        private void textBox13_MouseDown(object sender, MouseEventArgs e)
        {
            textBox13.Text = "Press key to use as hotkey";
        }

        private void textBox13_KeyDown(object sender, KeyEventArgs e)
        {
            textBox13.Text = e.KeyCode.ToString();
            SubscribeForHotkey(6);
        }

        private void textBox14_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.ControlKey)
            {
                var p = Cursor.Position;
                _SpecWepTwo = p;
                textBox14.Text = _SpecWepTwo.ToString();
            }
        }

        private void textBox15_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.ControlKey)
            {
                var p = Cursor.Position;
                _specbarPoint = p;
                textBox15.Text = _specbarPoint.ToString();
            }
        }

        private void textBox17_MouseDown(object sender, MouseEventArgs e)
        {
            textBox17.Text = "Press hotkey";
        }

        private void textBox18_MouseDown(object sender, MouseEventArgs e)
        {
            textBox18.Text = "Press CTRL on first prayer icon";
        }

        private void textBox18_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.ControlKey)
            {
                var p = Cursor.Position;
                _firstPrayer = p;
                textBox18.Text = _firstPrayer.ToString();
                if (!_stretched)
                {
                    GeneratePrayerBook(_firstPrayer, 40, 40);
                }
            }
        }

        private void textBox16_MouseDown(object sender, MouseEventArgs e)
        {
            textBox16.Text = "Press hotkey";
        }

        private void textBox21_MouseDown(object sender, MouseEventArgs e)
        {
            textBox21.Text = "Press hotkey";
        }

        private void textBox22_MouseDown(object sender, MouseEventArgs e)
        {
            textBox22.Text = "Press hotkey";
        }

        private void textBox23_MouseDown(object sender, MouseEventArgs e)
        {
            textBox23.Text = "Press hotkey";
        }

        private void textBox17_KeyDown(object sender, KeyEventArgs e)
        {
            textBox17.Text = e.KeyCode.ToString();
            _attackKey = e.KeyCode.ToString();
        }

        private void textBox16_KeyDown(object sender, KeyEventArgs e)
        {
            textBox16.Text = e.KeyCode.ToString();
            _prayerKey = e.KeyCode.ToString();
        }

        private void textBox21_KeyDown(object sender, KeyEventArgs e)
        {
            textBox21.Text = e.KeyCode.ToString();
            SubscribeForHotkey(7);
        }

        private void textBox22_KeyDown(object sender, KeyEventArgs e)
        {
            textBox22.Text = e.KeyCode.ToString();
            SubscribeForHotkey(8);
        }

        private void textBox23_KeyDown(object sender, KeyEventArgs e)
        {
            textBox23.Text = e.KeyCode.ToString();
            SubscribeForHotkey(9);
        }

        private void textBox24_MouseDown(object sender, MouseEventArgs e)
        {
            textBox24.Text = "Press hotkey";
        }

        private void textBox24_KeyDown(object sender, KeyEventArgs e)
        {
            textBox24.Text = e.KeyCode.ToString();
            SubscribeForHotkey(10);
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            _specOnePrayer = PrayerBook.GetNextPrayer(comboBox2.SelectedIndex);
        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            _specTwoPrayer = PrayerBook.GetNextPrayer(comboBox3.SelectedIndex);
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl1.SelectedIndex == 0)
            {
                Size = new Size(504, 395);
                foreach (var item in _inventoryAutoSwitcher)
                {
                    item.Enabled = item.Invent == InventoryItem.InventType.Item;
                }
                ResetSwitchCheckboxes();
                UpdateAutoswitcherUI();
            }
            else if (tabControl1.SelectedIndex == 1)
            {
                foreach (var item in _inventoryAutoSwitcher)
                {
                    item.Enabled = false;
                }

                ResetEatCheckboxes();
                Size = new Size(504, 395);
            }
            else if(tabControl1.SelectedIndex == 2)
            {
                Size = new Size(658, 395);
                ResetSpecialCheckboxes();
            }
            else
            {
                Size = new Size(504, 395);
            }
        }

        private void button10_Click(object sender, EventArgs e)
        {
            textBox12.Text = "";
            var currentIndex = 0;
            foreach (var inv in _inventoryAutoSwitcher)
            {
                if (inv.Enabled)
                {
                    _inventoryAutoSwitcher[currentIndex].Invent = InventoryItem.InventType.SpecWepOne;
                }
                else if (!inv.Enabled &&
                         _inventoryAutoSwitcher[currentIndex].Invent == InventoryItem.InventType.SpecWepOne)
                {
                    _inventoryAutoSwitcher[currentIndex].Invent = InventoryItem.InventType.None;
                }

                currentIndex++;
            }

            UpdateSpecHelperUI();
            UpdatePKHelperUI();
            UpdateAutoswitcherUI();
        }

        private void button14_Click(object sender, EventArgs e)
        {
            textBox14.Text = "";
            var currentIndex = 0;
            foreach (var inv in _inventoryAutoSwitcher)
            {
                if (inv.Enabled)
                {
                    _inventoryAutoSwitcher[currentIndex].Invent = InventoryItem.InventType.SpecWepTwo;
                }
                else if (!inv.Enabled &&
                         _inventoryAutoSwitcher[currentIndex].Invent == InventoryItem.InventType.SpecWepTwo)
                {
                    _inventoryAutoSwitcher[currentIndex].Invent = InventoryItem.InventType.None;
                }

                currentIndex++;
            }

            UpdateSpecHelperUI();
            UpdatePKHelperUI();
            UpdateAutoswitcherUI();
        }

        private void button15_Click(object sender, EventArgs e)
        {
            foreach (var inventoryItem in _inventoryAutoSwitcher)
            {
                if (inventoryItem.Invent == InventoryItem.InventType.SpecWepOne)
                {
                    inventoryItem.Invent = InventoryItem.InventType.None;
                }
            }

            UpdateSpecHelperUI();
            UpdatePKHelperUI();
        }

        private void button16_Click(object sender, EventArgs e)
        {
            foreach (var inventoryItem in _inventoryAutoSwitcher)
            {
                if (inventoryItem.Invent == InventoryItem.InventType.SpecWepTwo)
                {
                    inventoryItem.Invent = InventoryItem.InventType.None;
                }
            }

            UpdateSpecHelperUI();
            UpdatePKHelperUI();
        }

        private void checkBox63_CheckedChanged_1(object sender, EventArgs e)
        {
            _specPrayerOne = checkBox63.Checked;
            comboBox2.Enabled = checkBox63.Checked;
        }

        private void checkBox88_CheckedChanged(object sender, EventArgs e)
        {
            _specPrayerTwo = checkBox88.Checked;
            comboBox3.Enabled = checkBox88.Checked;
        }

        private void button11_Click(object sender, EventArgs e)
        {
            var currentIndex = 0;
            foreach (var inv in _inventoryAutoSwitcher)
            {
                if (inv.Enabled)
                {
                    _inventoryAutoSwitcher[currentIndex].Invent = InventoryItem.InventType.Item;
                }
                else if (!inv.Enabled && _inventoryAutoSwitcher[currentIndex].Invent == InventoryItem.InventType.Item)
                {
                    _inventoryAutoSwitcher[currentIndex].Invent = InventoryItem.InventType.None;
                }

                currentIndex++;
            }

            UpdateAutoswitcherUI();
            UpdatePKHelperUI();
            UpdateSpecHelperUI();
        }

        #region Checkboxes

        private void checkBox29_CheckedChanged(object sender, EventArgs e)
        {
            _oldPos = checkBox29.Checked;
        }


        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[0].Enabled = checkBox1.Checked;
        }

        private void checkBox8_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[1].Enabled = checkBox8.Checked;
        }

        private void checkBox15_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[2].Enabled = checkBox15.Checked;
        }

        private void checkBox16_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[3].Enabled = checkBox16.Checked;
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[4].Enabled = checkBox2.Checked;
        }

        private void checkBox9_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[5].Enabled = checkBox9.Checked;
        }

        private void checkBox17_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[6].Enabled = checkBox17.Checked;
        }

        private void checkBox23_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[7].Enabled = checkBox23.Checked;
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[8].Enabled = checkBox3.Checked;
        }

        private void checkBox10_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[9].Enabled = checkBox10.Checked;
        }

        private void checkBox18_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[10].Enabled = checkBox18.Checked;
        }

        private void checkBox24_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[11].Enabled = checkBox24.Checked;
        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[12].Enabled = checkBox4.Checked;
        }

        private void checkBox11_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[13].Enabled = checkBox11.Checked;
        }

        private void checkBox19_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[14].Enabled = checkBox19.Checked;
        }

        private void checkBox25_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[15].Enabled = checkBox25.Checked;
        }

        private void checkBox5_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[16].Enabled = checkBox5.Checked;
        }

        private void checkBox12_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[17].Enabled = checkBox12.Checked;
        }

        private void checkBox20_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[18].Enabled = checkBox20.Checked;
        }

        private void checkBox26_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[19].Enabled = checkBox26.Checked;
        }

        private void checkBox6_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[20].Enabled = checkBox6.Checked;
        }

        private void checkBox13_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[21].Enabled = checkBox13.Checked;
        }

        private void checkBox21_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[22].Enabled = checkBox21.Checked;
        }

        private void checkBox27_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[23].Enabled = checkBox27.Checked;
        }

        private void checkBox7_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[24].Enabled = checkBox7.Checked;
        }

        private void checkBox14_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[25].Enabled = checkBox14.Checked;
        }

        private void checkBox22_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[26].Enabled = checkBox22.Checked;
        }

        private void checkBox28_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[27].Enabled = checkBox28.Checked;
        }

        #endregion

        #region box

        private void checkBox58_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[0].Enabled = checkBox58.Checked;
        }

        private void checkBox51_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[1].Enabled = checkBox51.Checked;
        }

        private void checkBox44_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[2].Enabled = checkBox44.Checked;
        }

        private void checkBox43_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[3].Enabled = checkBox43.Checked;
        }

        private void checkBox57_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[4].Enabled = checkBox57.Checked;
        }

        private void checkBox50_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[5].Enabled = checkBox50.Checked;
        }

        private void checkBox42_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[6].Enabled = checkBox42.Checked;
        }

        private void checkBox36_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[7].Enabled = checkBox36.Checked;
        }

        private void checkBox56_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[8].Enabled = checkBox56.Checked;
        }

        private void checkBox49_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[9].Enabled = checkBox49.Checked;
        }

        private void checkBox41_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[10].Enabled = checkBox41.Checked;
        }

        private void checkBox35_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[11].Enabled = checkBox35.Checked;
        }

        private void checkBox55_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[12].Enabled = checkBox55.Checked;
        }

        private void checkBox48_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[13].Enabled = checkBox48.Checked;
        }

        private void checkBox40_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[14].Enabled = checkBox40.Checked;
        }

        private void checkBox34_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[15].Enabled = checkBox34.Checked;
        }

        private void checkBox54_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[16].Enabled = checkBox54.Checked;
        }

        private void checkBox47_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[17].Enabled = checkBox47.Checked;
        }

        private void checkBox39_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[18].Enabled = checkBox39.Checked;
        }

        private void groupBox3_Enter(object sender, EventArgs e)
        {
        }

        private void checkBox33_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[19].Enabled = checkBox33.Checked;
        }

        private void checkBox53_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[20].Enabled = checkBox53.Checked;
        }

        private void checkBox46_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[21].Enabled = checkBox46.Checked;
        }

        private void checkBox38_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[22].Enabled = checkBox38.Checked;
        }

        private void checkBox32_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[23].Enabled = checkBox32.Checked;
        }

        private void checkBox52_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[24].Enabled = checkBox52.Checked;
        }

        private void checkBox45_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[25].Enabled = checkBox45.Checked;
        }

        private void checkBox37_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[26].Enabled = checkBox37.Checked;
        }

        private void checkBox31_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[27].Enabled = checkBox31.Checked;
        }

        #endregion


        #region Textboxmanip

        private void textBox6_MouseDown(object sender, MouseEventArgs e)
        {
            textBox6.Text = "Press key to use as hotkey";
            SubscribeForHotkey(2);
        }

        private void textBox6_KeyDown(object sender, KeyEventArgs e)
        {
            textBox6.Text = e.KeyCode.ToString();
        }

        private void textBox7_MouseDown(object sender, MouseEventArgs e)
        {
            textBox7.Text = "Press key to use as hotkey";
            SubscribeForHotkey(3);
        }

        private void textBox7_KeyDown(object sender, KeyEventArgs e)
        {
            textBox7.Text = e.KeyCode.ToString();
        }

        private void textBox9_KeyDown(object sender, KeyEventArgs e)
        {
            textBox9.Text = e.KeyCode.ToString();
        }

        private void textBox9_MouseDown(object sender, MouseEventArgs e)
        {
            textBox9.Text = "Press key to use as hotkey";
            SubscribeForHotkey(4);
        }

        private void textBox8_MouseDown(object sender, MouseEventArgs e)
        {
            textBox8.Text = "Press key to use as hotkey";
            SubscribeForHotkey(5);
        }

        private void textBox8_KeyDown(object sender, KeyEventArgs e)
        {
            textBox8.Text = e.KeyCode.ToString();
        }

        #endregion

        #region CheckboxesSpec

        private void checkBox87_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[0].Enabled = checkBox87.Checked;
            Console.WriteLine(_inventoryAutoSwitcher[0].Enabled);
        }

        private void checkBox80_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[1].Enabled = checkBox80.Checked;
        }

        private void checkBox73_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[2].Enabled = checkBox73.Checked;
        }

        private void checkBox72_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[3].Enabled = checkBox72.Checked;
        }

        private void checkBox86_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[4].Enabled = checkBox86.Checked;
        }

        private void checkBox79_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[5].Enabled = checkBox79.Checked;
        }

        private void checkBox71_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[6].Enabled = checkBox71.Checked;
        }

        private void checkBox65_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[7].Enabled = checkBox87.Checked;
        }

        private void checkBox85_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[8].Enabled = checkBox85.Checked;
        }

        private void checkBox78_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[9].Enabled = checkBox78.Checked;
        }

        private void checkBox70_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[10].Enabled = checkBox70.Checked;
        }

        private void checkBox64_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[11].Enabled = checkBox64.Checked;
        }

        private void checkBox84_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[12].Enabled = checkBox84.Checked;
        }

        private void checkBox77_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[13].Enabled = checkBox77.Checked;
        }

        private void checkBox69_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[14].Enabled = checkBox69.Checked;
        }

        private void checkBox62_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[15].Enabled = checkBox62.Checked;
        }

        private void checkBox83_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[16].Enabled = checkBox83.Checked;
        }

        private void checkBox76_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[17].Enabled = checkBox76.Checked;
        }

        private void checkBox68_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[18].Enabled = checkBox68.Checked;
        }

        private void checkBox61_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[19].Enabled = checkBox61.Checked;
        }

        private void checkBox82_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[20].Enabled = checkBox82.Checked;
        }

        private void checkBox75_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[21].Enabled = checkBox75.Checked;
        }

        private void checkBox67_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[22].Enabled = checkBox67.Checked;
        }

        private void checkBox60_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[23].Enabled = checkBox60.Checked;
        }

        private void checkBox81_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[24].Enabled = checkBox81.Checked;
        }

        private void checkBox74_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[25].Enabled = checkBox74.Checked;
        }

        private void checkBox66_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[26].Enabled = checkBox66.Checked;
        }

        private void checkBox59_CheckedChanged(object sender, EventArgs e)
        {
            _inventoryAutoSwitcher[27].Enabled = checkBox59.Checked;
        }

        #endregion
        System.Timers.Timer t = new System.Timers.Timer();
        private bool timerStarted = false;
        private void button12_Click(object sender, EventArgs e)
        {
            if (!timerStarted)
            {
                Random r = new Random();
                t.Interval = r.Next(6000, 8000);
                t.Elapsed += TOnElapsed;
                t.Start();
                timerStarted = true;
                button12.Text = "Stop";
            }
            else
            {
                t.Stop();
                timerStarted = false;
                button12.Text = "Start";
            }

        }

        private void TOnElapsed(object sender, ElapsedEventArgs e)
        {
            Random r = new Random();
            MouseEvents.LinearSmoothMove(prayer,r.Next(20,40),true);
            Thread.Sleep(r.Next(400,500));
            MouseEvents.LinearSmoothMove(prayer, r.Next(10, 20), true);
            t.Interval = r.Next(8000, 25000);
        }

        private void SmiteOn(object sender, HotkeyEventArgs e)
        {
            UsePrayer(PrayerBook.Prayer.Smite);
        }
        private void MeleeOn(object sender, HotkeyEventArgs e)
        {
            UsePrayer(PrayerBook.Prayer.ProtectfromMelee);
        }
        private void RangeOn(object sender, HotkeyEventArgs e)
        {
            UsePrayer(PrayerBook.Prayer.ProtectfroMissiles);
        }
        private void MageOn(object sender, HotkeyEventArgs e)
        {
            UsePrayer(PrayerBook.Prayer.ProtectfromMagic);
        }


        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private Point prayer;
        private void textBox20_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.ControlKey)
            {
                var p = Cursor.Position;
                prayer = p;
                textBox20.Text = p.ToString();

            }
        }
    }
}