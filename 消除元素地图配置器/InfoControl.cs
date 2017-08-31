using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace 消除元素地图配置器
{
    public partial class InfoControl : UserControl
    {
        public string _ImageInfo;

        public delegate void DeleteImgDlg(string str);
        public DeleteImgDlg DeleteImg;

        public delegate void UpdateImageStateDlg(string info,long state);
        public UpdateImageStateDlg UpdateImageState;

        public InfoControl()
        {
            InitializeComponent();
            _ImageInfo = "";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DeleteImg(_ImageInfo);
        }

      
        private void textBox2_KeyDown(object sender, KeyEventArgs e)
        {
            return;
            //System.Text.RegularExpressions.Regex rex =new System.Text.RegularExpressions.Regex(@"^\d+$");
            //if (e.KeyCode == Keys.Enter && rex.IsMatch(textBox2.Text))
            //{
            //    UpdateImageState(_ImageInfo, Convert.ToInt64(textBox2.Text));
            //    MessageBox.Show("状态更新成功");
            //}
        }
    }
}
