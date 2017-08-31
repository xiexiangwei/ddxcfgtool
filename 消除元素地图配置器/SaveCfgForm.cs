using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace 消除元素地图配置器
{
    public partial class SaveCfgForm : Form
    {

        public delegate void SaveConfigDlg(string name);
        public SaveConfigDlg SaveConfig;
        public SaveCfgForm()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if(textBox1.Text!="")
            {
                SaveConfig(textBox1.Text);
            }
        }
    }
}
