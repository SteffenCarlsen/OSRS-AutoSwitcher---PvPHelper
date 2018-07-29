using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AutoSwitcher
{
    public partial class NewPreset : Form
    {
        public string name = "";
        public Button button { get; set; }
        public NewPreset()
        {
            InitializeComponent();
            button = button1;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            name = textBox1.Text;
            Close();
        }
    }
}
