using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace 消除元素地图配置器
{
    public partial class ExportForm : Form
    {
        
        public delegate string GetJsonDelegate(int x,int y);
        public GetJsonDelegate GetJsonFunc;
        
        public ExportForm()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {   
            try
            {
                string json = GetJsonFunc(Convert.ToInt32(comboBox2.Text), Convert.ToInt32(comboBox1.Text));
                ExportJson(json);
                this.Close();
            }
            catch(Exception err)
            {
                MessageBox.Show(err.Message);
            }
          
        }

        private void ExportJson(string json)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "JSON文件（*.json）|";
            sfd.FilterIndex = 0;
            sfd.RestoreDirectory = true;
            sfd.DefaultExt = "json";
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                string localFilePath = sfd.FileName.ToString(); //获得文件路径 
                try
                {
                    using (StreamWriter writer = new StreamWriter(localFilePath,false))
                    {
                        writer.Flush();
                        writer.Write(json);
                    }
                }
                catch (IOException ex)
                {
                    MessageBox.Show(ex.Message, "Simple Editor", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
            }
        }
    }
}
