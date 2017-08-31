using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Data.SQLite;
using System.IO;
using System.Drawing.Imaging;

namespace 消除元素地图配置器
{
    public partial class MainForm : Form
    {
        //imagelist中图片索引对应图片ID
        Dictionary<int, ImageKeys> _imgIndexImgIDMap;
        //元素缓存map
        Dictionary<ImageKeys, stImage> _imgMap;
        //委托
        delegate void UpdateJsonStrDlg();
        UpdateJsonStrDlg UpdateJsonStr;
        delegate void ResetMapDlg();
        ResetMapDlg ResetMap;
        delegate string GetJsonDlg(int x,int y);
        GetJsonDlg GetJson;

        //当前选中元素所在ImageList控件中的索引
        Int64 _cur_ID;
        int _cur_Image_ID;
        int _cur_Image_Index;
        Int64 _cur_Image_level;
        Int64 _cur_Image_State;
        string _cur_Image_Name;
        //加载配置相关数据
        Int64 _cur_config_id;
        string _cur_config_name;

        SaveCfgForm _saveCfgForm;
        Dictionary<long, stConfig> _configMap;
        //sqlite 路径
        string _sqlCon;
        public MainForm()
        {
            InitializeComponent();
            _sqlCon = "Data Source=" + System.IO.Directory.GetCurrentDirectory() + "\\MyDatabase.sqlite";
            _imgIndexImgIDMap = new Dictionary<int, ImageKeys>();
            _cur_ID = -1;
            _cur_Image_ID = -1;
            _imgMap = new Dictionary<ImageKeys, stImage>();
            _cur_Image_Index = -1;
            _saveCfgForm = null;
            _configMap = new Dictionary<long, stConfig>();
            _cur_config_id = -1;
            _cur_config_name = "";
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            imageList1.Images.Clear();
            List<stImage> imgList = DB_LoadImage();
            for (int n = 0; n < imgList.Count; n++)
            {
                imageList1.Images.Add(imgList[n].Img);
                var new_key = new ImageKeys(imgList[n].Image_ID, imgList[n].Image_State);
                //ImageList控件上的图片对应DB的ID
                _imgIndexImgIDMap.Add(n, new_key);
                //缓存图片信息
                _imgMap.Add(new_key, imgList[n]);

            }
            RefreshListView();
            InitConfigItem();
            UpdateJsonStr = mapControl1.UpdateJsonStr;
            ResetMap = mapControl1.ResetMap;
            GetJson = mapControl1.ImageArray2Json;
            mapControl1.SetImageList(imageList1);
            mapControl1.SetImageIndex(-1);
            mapControl1._UpdateJson = UpdateJsonText;
            mapControl1._GetImageByID = DB_GetImageByImageID;
            mapControl1._OnClickPB = OnClickPb;
            InitJsonText();
            textBox4.SelectionStart = 0;
            textBox4.SelectionLength = 0;

        }
        //json文件预览
        void InitJsonText()
        {
            string text = "\"data\":\r\n[\r\n";
            for (int x = 0; x < 9; x++)
            {
                text += "[";
                for (int y = 0; y < 9; y++)
                {
                    text += string.Format("[\"{0}\"]", -1);
                    if (y != 8)
                    {
                        text += ",";
                    }
                }
                text += "]";
                if (x != 8)
                {
                    text += ",\r\n";
                }
            }
            text += "\r\n]";
            textBox4.Text = text;
        }

        void UpdateJsonText(string str)
        {
            textBox4.Text = str;
            textBox4.SelectionStart = 0;
            textBox4.SelectionLength = 0;
        }

        public static byte[] ImageToBytes(Image image)
        {
            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    using (Bitmap bmp = new Bitmap(image))
                    {
                        bmp.Save(ms, image.RawFormat);
                    }
                    return ms.ToArray();
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
            return null;

        }


        public static Image BytesToImage(byte[] buffer)
        {
            MemoryStream ms = new MemoryStream(buffer);
            Image image = System.Drawing.Image.FromStream(ms);
            return image;
        }

        private void RefreshListView()
        {
            panel1.Controls.Clear();
            //创建TabControl
            TabControl tbc = new TabControl();
            tbc.Size = new Size(356, 264);
            panel1.Controls.Add(tbc);
            for (int i = 0; i < imageList1.Images.Count; i++)
            {
                if (_imgIndexImgIDMap.ContainsKey(i))
                {
                    var image_key = _imgIndexImgIDMap[i];
                    var image_id = (int)image_key.Image_ID;
                    var image_state = image_key.Image_State;


                    stImage imgInfo = DB_GetImageByImageID(image_id, image_key.Image_State);
                    string level = imgInfo.Image_Level.ToString();
                    //添加 TabPage
                    TabPage tbp;
                    var levelPages = tbc.Controls.Find(level, false);
                    if (levelPages.Length > 0)
                    {
                        tbp = levelPages[0] as TabPage;
                    }
                    else
                    {
                        tbp = new TabPage();
                        tbp.Name = imgInfo.Image_Level.ToString();
                        tbp.Text = string.Format("第{0}层", imgInfo.Image_Level);
                        //tbp.UseVisualStyleBackColor = true;
                        tbc.Controls.Add(tbp);
                        //添加 ListView
                        ListView lv = new ListView();
                        lv.Dock = DockStyle.Fill;
                        lv.Name = "lv";
                        lv.LargeImageList = imageList1;
                        lv.Click += new System.EventHandler(this.lv_Click);
                        lv.SelectedIndexChanged += new EventHandler(lv_SelectItemChange);
                        tbp.Controls.Add(lv);
                    }
                    //绑定图片到 ListView
                    ListView curLv = tbp.Controls.Find("lv", false)[0] as ListView;
                    int count = curLv.Items.Count;
                    curLv.Items.Add(System.IO.Path.GetFileName(imageList1.Images[i].ToString()), i);
                    curLv.Items[count].ImageIndex = i;
                    curLv.Items[count].Text = imgInfo.Image_Name == "" || imgInfo.Image_Name==null ? image_id.ToString() : imgInfo.Image_Name;
                    //元素name对应数据库中的ID
                    curLv.Items[count].Name = string.Format("{0}%{1}", image_id, image_state);
                }
            }
        }

        private void OnClickPb(int x, int y, SortedDictionary<int, ImageProperty> imgmap)
        {   
            //画格子信息
            Label posLbl;
            var posLblList = panel5.Controls.Find("posLabel", false);
            if (posLblList.Length == 0)
            {
                posLbl = new Label();
                posLbl.Name = "posLabel";
                posLbl.Text = string.Format("格子[{0}:{1}]", x, y);
                posLbl.Location = new Point(80, 5);
                posLbl.ForeColor = Color.Green;
                posLbl.Font = new Font("SimSun", 12);
                panel5.Controls.Add(posLbl);
            }
            else
            {
                posLbl = posLblList[0] as Label;
            }
            posLbl.Text = string.Format("格子[{0}:{1}]", x, y);


            //画层级信息
            var lblNoList = panel5.Controls.Find("lblNo", false);
            if (imgmap.Count == 0)
            {
                Label lblNo;
                if (lblNoList.Length == 0)
                {
                    lblNo = new Label();
                    lblNo.Name = "lblNo";
                    lblNo.AutoSize = true;
                    lblNo.Text = "没有配置元素";
                    posLbl.Font = new Font("SimSun", 12);
                    lblNo.ForeColor = Color.Red;
                    lblNo.Location = new Point(0, 30);
                    panel5.Controls.Add(lblNo);
                }
                List<InfoControl> icList = new List<InfoControl>();
                foreach (var c in panel5.Controls)
                {
                    InfoControl ic = c as InfoControl;
                    if (ic != null)
                    {
                        icList.Add(ic);
                    }
                }
                foreach (var ic in icList)
                {
                    panel5.Controls.Remove(ic);
                }
            }
            else
            {
                if (lblNoList.Length != 0)
                {
                    panel5.Controls.Remove(lblNoList[0]);
                }
                List<int> sortkeys = new List<int>();
                foreach(var v in imgmap)
                {
                    sortkeys.Add(v.Key);
                }
                sortkeys.Sort();

                int n = 0;
                foreach (var key in sortkeys)
                {
                    var img = imgmap[key];
                    InfoControl ic;
                    stImage imgInfo = DB_GetImageByImageID((int)img.Image_ID, img.Image_State);
                    var icList = panel5.Controls.Find(n.ToString(), false);
                    if (icList.Length == 0)
                    {
                        ic = new InfoControl();
                        ic.Name = n.ToString();
                        ic.Location = new Point(10, n * 90 + (n + 1) * 30);
                        ic.label1.Text = string.Format("第{0}层:", key);
                        ic.textBox1.Text = imgInfo.Image_ID.ToString();
                        ic.textBox2.Text = img.Image_State.ToString();
                        ic.pictureBox1.Image = imageList1.Images[img.Image_Index];
                        ic._ImageInfo = string.Format("{0}:{1}:{2}:{3}", x, y, imgInfo.Image_ID, imgInfo.Image_Level);
                        ic.DeleteImg = mapControl1.DeleteImg;
                        panel5.Controls.Add(ic);
                    }
                    else
                    {
                        ic = icList[0] as InfoControl;
                        ic.label1.Text = string.Format("第{0}层:", key);
                        ic.textBox1.Text = imgInfo.Image_ID.ToString();
                        ic.textBox2.Text = img.Image_State.ToString();
                        ic.pictureBox1.Image = imageList1.Images[img.Image_Index];
                        ic._ImageInfo = string.Format("{0}:{1}:{2}:{3}", x, y, imgInfo.Image_ID, imgInfo.Image_Level);
                    }
                    n++;
                }

                List<InfoControl> ifocList = new List<InfoControl>();
                foreach (var c in panel5.Controls)
                {
                    InfoControl ic = c as InfoControl;
                    if (ic != null)
                    {
                        ifocList.Add(ic);
                    }

                }
                foreach (var ic in ifocList)
                {
                    if (Convert.ToInt32(ic.Name) >= imgmap.Count)
                    {
                        panel5.Controls.Remove(ic);
                    }
                }
            }
        }

        private void lv_Click(object sender, EventArgs e)
        {
            ListView lv = ((ListView)sender);
            if (lv.SelectedIndices != null && lv.SelectedIndices.Count > 0)
            {
                ListView.SelectedIndexCollection c = lv.SelectedIndices;
                string[] key = lv.Items[c[0]].Name.Split('%');

                stImage selectImg = DB_GetImageByImageID(Convert.ToInt32(key[0]), Convert.ToInt32(key[1]));
                _cur_ID = selectImg.ID;
                _cur_Image_ID = (int)selectImg.Image_ID;
                _cur_Image_Index = lv.Items[c[0]].ImageIndex;
                _cur_Image_Name = selectImg.Image_Name;
                _cur_Image_level = selectImg.Image_Level;
                _cur_Image_State = selectImg.Image_State;

                mapControl1.SetImageIndex((int)_cur_Image_Index);
                mapControl1.SetImage(selectImg);

                textBox_ImageID.Text = Convert.ToString(selectImg.Image_ID);
                textBox_State.Text = selectImg.Image_State.ToString();
                textBox_Level.Text = selectImg.Image_Level.ToString();
                textBox_Name.Text = selectImg.Image_Name;
                pictureBox1.Image = imageList1.Images[lv.Items[c[0]].ImageIndex];
            }
        }

        private void lv_SelectItemChange(object sender, EventArgs e)
        {
            ListView lv = ((ListView)sender);
            if (lv.SelectedIndices == null || lv.SelectedIndices.Count == 0)
            {
                ClearSelectedInfo();
            }
        }

        void ClearSelectedInfo()
        {
            _cur_Image_Index = -1;
            _cur_ID = -1;
            mapControl1.SetImageIndex(-1);
            textBox_ImageID.Text = "";
            textBox_State.Text = "";
            textBox_Level.Text = "";
            pictureBox1.Image = null;
        }

        protected bool IsNumberic(string str)
        {
            System.Text.RegularExpressions.Regex rex =
            new System.Text.RegularExpressions.Regex(@"^[+-]?\d+$");
            if (rex.IsMatch(str))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        void InitConfigItem()
        {
            配置列表AToolStripMenuItem.DropDownItems.Clear();
            删除配置DToolStripMenuItem.DropDownItems.Clear();
            _configMap = DB_LoadConfig();
            foreach (var config in _configMap)
            {
                ToolStripMenuItem subItem = new ToolStripMenuItem();
                subItem.Name = config.Key.ToString();
                subItem.Text = config.Value.Name;
                subItem.Click += new EventHandler(OnConfigClick);
                配置列表AToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { subItem });
            }
            foreach (var config in _configMap)
            {
                ToolStripMenuItem subItem = new ToolStripMenuItem();
                subItem.Name = config.Key.ToString();
                subItem.Text = string.Format("删除 {0}", config.Value.Name);
                subItem.Click += new EventHandler(OnDeleteConfig);
                删除配置DToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { subItem });
            }
        }

        private void OnConfigClick(object sender, EventArgs e)
        {
            Int64 key = Convert.ToInt64(((ToolStripMenuItem)sender).Name);
            if (_configMap.ContainsKey(key))
            {
                string config = _configMap[key].Config;
                ResetByConfig(config);
                _cur_config_id = key;
                _cur_config_name = _configMap[key].Name;
            }
        }

        private void OnDeleteConfig(object sender, EventArgs e)
        {
            Int64 key = Convert.ToInt64(((ToolStripMenuItem)sender).Name);
            if (_configMap.ContainsKey(key))
            {
                if (MessageBox.Show(string.Format("确定要删除配置:{0}?", _configMap[key].Name), "提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
                {
                    DB_DeleteConfig(key);
                    InitConfigItem();

                    _cur_config_id = -1;
                    _cur_config_name = "";
                }
            }
        }


        private int GetImageIndexByImgId(Int64 imageid,Int64 imgstate)
        {
            int imgIndex = 0;
            foreach (var info in _imgIndexImgIDMap)
            {
                if (info.Value.Image_ID == imageid && info.Value.Image_State == imgstate)
                { imgIndex = info.Key; }
            }
            return imgIndex;
        }

        void ResetByConfig(string config)
        {
            try
            {
                //解析每个元素
                SortedDictionary<int, ImageProperty>[,] imageArray = new SortedDictionary<int, ImageProperty>[9, 9];
                for (int i = 0; i < 9; i++)
                {
                    for (int j = 0; j < 9; j++)
                    {
                        imageArray[i, j] = new SortedDictionary<int, ImageProperty>();
                    }
                }
                string[] xCfgArray = config.Substring(7).Replace("]]", "@").Split('@');
                int x = 0;
                for (int n = 0; n < xCfgArray.Length; n++)
                {
                    if (n == 9) break;
                    int y = 0;
                    string[] subXcfgArray = xCfgArray[n].Replace(",[", "@").Split('@');
                    for (int k = 0; k < subXcfgArray.Length; k++)
                    {
                        if (subXcfgArray[k] == "") continue;
                        string content = "";
                        int offset = 0;
                        if (k == 0 && n == 0) offset = 7;
                        if (k == 0 && n != 0) offset = 5;
                        string tempXCfg = subXcfgArray[k].Substring(offset);
                        content = tempXCfg.IndexOf(']') >= 0 ? tempXCfg.Remove(tempXCfg.IndexOf(']')) : tempXCfg;
                        string[] levelList = content.Split(',');
                        for (int m = 0; m < levelList.Length; m++)
                        {
                            string[] info = levelList[m].Split(':');
                            if (info.Length == 1 && (info[0]=="-1\""|| info[0]=="\"-1\"")) break;
                            int offset2 = 0;
                            if (info.Length >= 2)
                            {
                                offset2 = 1;
                            }
                            ImageProperty ip = new ImageProperty();
                            ip.Image_ID = info.Length == 2 ? Convert.ToInt64(info[0].Substring(offset2)) : Convert.ToInt64(info[0].Substring(1).Remove(info[0].Substring(1).IndexOf('"')));
                            ip.Image_State = info.Length == 2 ? Convert.ToInt64(info[1].Remove(info[1].IndexOf('"'))) : Convert.ToInt64(Const.NoState);
                            ip.Image_Index = GetImageIndexByImgId(ip.Image_ID, ip.Image_State);
                            stImage imgInfo = DB_GetImageByImageID((int)ip.Image_ID, ip.Image_State);
                            imageArray[x, y].Add((int)imgInfo.Image_Level, ip);
                        }
                        y++;
                    }
                    x++;
                }

                mapControl1.LoadConfig(imageArray);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }


        public string GetJsonFun(int x,int y)
        {
            return GetJson(x, y);
        }

        #region Sqlite 数据库操作函数
        private void DB_SaveImage(Int64 image_id, Image img)
        {
            using (SQLiteConnection conn = new SQLiteConnection(_sqlCon))
            {
                conn.Open();
                using (SQLiteCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = string.Format("insert into tb_image('image_id','image') values({0},@image_buf)", image_id);
                    SQLiteParameter para = new SQLiteParameter("@image_buf", DbType.Binary);
                    para.Value = ImageToBytes(img);
                    cmd.Parameters.Add(para);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private Int64 DB_GetMaxImgID()
        {
            Int64 max_id = 0;
            using (SQLiteConnection conn = new SQLiteConnection(_sqlCon))
            {
                conn.Open();
                using (SQLiteCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "select * from sqlite_sequence where name='tb_image'";
                    SQLiteDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        max_id = (Int64)reader["seq"];
                    }
                }
            }
            return max_id;
        }


        private List<stImage> DB_LoadImage()
        {
            List<stImage> imgList = new List<stImage>();

            using (SQLiteConnection conn = new SQLiteConnection(_sqlCon))
            {
                conn.Open();
                string sql = "select * from tb_image";
                SQLiteCommand command = new SQLiteCommand(sql, conn);
                SQLiteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    stImage image = new stImage();
                    image.ID = (Int64)reader["id"];
                    image.Img = BytesToImage((byte[])reader["image"]); ;
                    image.Image_ID = (Int64)reader["image_id"];
                    image.Image_State = (Int64)reader["image_state"];
                    image.Image_Level = (Int64)reader["image_level"];
                    image.Image_Name = reader["image_name"] == null ? "" : reader["image_name"].ToString();
                    imgList.Add(image);
                }
            }
            return imgList;
        }

        private Dictionary<long, stConfig> DB_LoadConfig()
        {
            Dictionary<long, stConfig> cfgMap = new Dictionary<long, stConfig>();

            using (SQLiteConnection conn = new SQLiteConnection(_sqlCon))
            {
                conn.Open();
                string sql = "select * from tb_config";
                SQLiteCommand command = new SQLiteCommand(sql, conn);
                SQLiteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    stConfig c = new stConfig();
                    c.Name = reader["name"].ToString();
                    c.Config = reader["config"].ToString();
                    cfgMap.Add((Int64)reader["id"], c);
                }
            }
            return cfgMap;
        }


        private void DB_DeleteConfig(Int64 id)
        {

            ExecuteSql(string.Format("delete from tb_config where id={0}", id));
        }

        private void DB_SaveConfig(string name)
        {   

                ExecuteSql(string.Format("insert into tb_config('name','config') values('{0}','{1}')", name, @textBox4.Text));
                _saveCfgForm.Close();
                _saveCfgForm = null;
                InitConfigItem();
                MessageBox.Show("配置保存成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private stImage DB_GetImageByImageID(int image_id,Int64 image_state)
        {
            stImage img = new stImage();
            var k = new ImageKeys(image_id, image_state);

            if (_imgMap.ContainsKey(k))
            {
                img = _imgMap[k];
            }
            else
            {
                using (SQLiteConnection conn = new SQLiteConnection(_sqlCon))
                {
                    conn.Open();
                    string sql =string.Format("select * from tb_image where image_id={0} and image_state={1}",image_id,image_state);
                    SQLiteCommand command = new SQLiteCommand(sql, conn);
                    SQLiteDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        img.ID = (Int64)reader["id"];
                        img.Img = BytesToImage((byte[])reader["image"]);
                        img.Image_ID = (Int64)reader["image_id"];
                        img.Image_State = (Int64)reader["image_state"];
                        img.Image_Level = (Int64)reader["image_level"];
                        img.Image_Name = reader["image_name"] == null ? "" : reader["image_name"].ToString();
                        var new_key = new ImageKeys(img.Image_ID, img.Image_State);
                        _imgMap[new_key] = img;
                    }
                }
            }
            return img;
        }

        private void DB_DeleteImage(ImageKeys k)
        {
            using (SQLiteConnection conn = new SQLiteConnection(_sqlCon))
            {
                conn.Open();
                string sql = string.Format("delete from tb_image where image_id ={0} and image_state={1}",k.Image_ID,k.Image_State);
                SQLiteCommand command = new SQLiteCommand(sql, conn);
                command.ExecuteNonQuery();
            }
        }

        private bool DB_IsImgIDExist(Int64 imgid,Int64 imgstate)
        {
            using (SQLiteConnection conn = new SQLiteConnection(_sqlCon))
            {
                try
                {
                    conn.Open();
                    string sql = string.Format("select count(*) from tb_image where image_id={0} and image_state={1}", imgid, imgstate);
                    SQLiteCommand command = new SQLiteCommand(sql, conn);
                    SQLiteDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        if (Convert.ToInt32(reader["count(*)"]) > 0)
                        {
                            reader.Close();
                            return true;
                        }
                    }
                    reader.Close();
                    conn.Close();
                }
                catch (System.Data.SQLite.SQLiteException E)
                {
                    conn.Close();
                    throw new Exception(E.Message);
                }

            }
            return false;
        }
        #endregion 
        #region UI事件
        private void button_Add_Click(object sender, EventArgs e)
        {
            //设置打开文件控件  
            OpenFileDialog openfile = new OpenFileDialog();
            openfile.Filter = "JPG(*.JPG;*.JPEG);gif文件(*.GIF);BMP文件(*.BMP);PNG文件(*.PNG)|*.jpg;*.jpeg;*.gif;*.bmp;*.png";
            openfile.FilterIndex = 1;  //当前选定索引  
            openfile.RestoreDirectory = true;
            openfile.FileName = "";
            //对话框选择确定按钮  
            if (openfile.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    //FromFile从指定的文件创建Image  
                    Image img = Image.FromFile(openfile.FileName);
                    //图片加载到ImageList控件和imageList图片列表  
                    Int64 img_id = DB_GetMaxImgID() + 1;
                    DB_SaveImage(img_id, img);
                    var new_key = new ImageKeys(img_id, 1);
                    _imgIndexImgIDMap.Add(imageList1.Images.Count, new_key);
                    imageList1.Images.Add(img);
                    //缓存图片信息
                    stImage newImg = new stImage();
                    newImg.ID = img_id;
                    newImg.Image_ID = img_id;
                    newImg.Image_State = 1;
                    _imgMap.Add(new_key, newImg);
                    RefreshListView();
                }
                catch (Exception error)
                {
                    MessageBox.Show(error.Message);
                }

            }
        }

        private void button_Delete_Click(object sender, EventArgs e)
        {
            if (textBox_ImageID.Text == "" || _cur_Image_Index == -1)
            {
                return;
            }
            if (MessageBox.Show(string.Format("确定要删除元素{0}-{1}？", textBox_ImageID.Text, textBox_State.Text), "提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK)
            {
                var cur_key = new ImageKeys(Convert.ToInt64(textBox_ImageID.Text), Convert.ToInt64(textBox_State.Text));
                //数据库中删除图片
                DB_DeleteImage(_imgIndexImgIDMap[_cur_Image_Index]);
                _imgIndexImgIDMap.Remove(_cur_Image_Index);
                RefreshListView();
                ClearSelectedInfo();
            }
        }


        private void 自定义CToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ResetMap();
            UpdateJsonStr();
            panel5.Controls.Clear();
            _cur_config_id = -1;
            _cur_config_name = "";
        }

        private void 保存配置CToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_cur_config_id != -1)
            {
                if (MessageBox.Show(string.Format("确定要覆盖原有配置({0})吗？", _cur_config_name), "覆盖配置", MessageBoxButtons.OKCancel, MessageBoxIcon.Asterisk) == DialogResult.OK)
                {
                    ExecuteSql(string.Format("update  tb_config set config= '{0}' where id={1}", @textBox4.Text, _cur_config_id));
                    InitConfigItem();
                    MessageBox.Show("配置覆写成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                _saveCfgForm = new SaveCfgForm();
                _saveCfgForm.SaveConfig = DB_SaveConfig;
                _saveCfgForm.Show();
            }
        }

        private void 保存SToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ExportForm f = new ExportForm();
            f.GetJsonFunc = GetJsonFun;
            f.Show();
        }


        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (MessageBox.Show("确定要退出程序吗?", "退出程序", MessageBoxButtons.OKCancel) == DialogResult.Cancel)
            {
                e.Cancel = true;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {   
            try
            {
                if (!IsNumberic(textBox_ImageID.Text))
                {
                    MessageBox.Show("元素ID不能包含字符串!");
                    return;
                }
                var image_id = Convert.ToInt64(textBox_ImageID.Text);

                if (!IsNumberic(textBox_State.Text))
                {
                    MessageBox.Show("元素状态不能包含字符串!");
                    return;
                }
                var image_state = Convert.ToInt64(textBox_State.Text);
                var new_key = new ImageKeys(image_id, image_state);
                var old_key = new ImageKeys(_cur_Image_ID, _cur_Image_State);


                if (!IsNumberic(textBox_Level.Text))
                {
                    MessageBox.Show("元素层级不能包含字符串!");
                    return;
                }
                if (!_imgMap.ContainsKey(old_key))
                {
                    MessageBox.Show("当前选中元素不存在!");
                    return;
                }

                string sql = "";

                if (_cur_Image_ID != Convert.ToInt64(textBox_ImageID.Text) || _cur_Image_State!= Convert.ToInt64(textBox_State.Text))
                {
                    if (DB_IsImgIDExist(image_id, image_state))
                    {
                        MessageBox.Show(string.Format("该键值({0}-{1})已经存在!",image_id,image_state));
                        return;
                    }
                    var temp_img_info = _imgMap[old_key];
                    temp_img_info.Image_ID = image_id;
                    temp_img_info.Image_State = image_state;
                    _imgMap.Add(new_key, temp_img_info);
                 
                    _imgMap.Remove(old_key);
                    _imgIndexImgIDMap[(int)_cur_Image_Index] = new_key;
                    sql += string.Format("update tb_image set image_id={0},image_state={1} where id={2};", image_id, image_state,_cur_ID);
                   
                }

               
                if (_cur_Image_Name != textBox_Name.Text)
                {
                    _imgMap[new_key].Image_Name = textBox_Name.Text;
                    sql += string.Format("update tb_image set image_name='{0}' where id={1};", textBox_Name.Text, _cur_ID);
                }
                bool change_level = false;
                if (_cur_Image_level != Convert.ToInt64(textBox_Level.Text))
                {
                    _imgMap[new_key].Image_Level = Convert.ToInt64(textBox_Level.Text);
                    sql += string.Format("update tb_image set image_level='{0}' where id={1};", Convert.ToInt64(textBox_Level.Text), _cur_ID);
                    change_level = true;
                }

                if(sql!="")
                {
                    ExecuteSql(sql);
                    if (change_level)
                    {
                        RefreshListView();
                    }
                    else
                    {
                        UpdateImageInfo(old_key, new_key, textBox_Name.Text);
                    }
                    mapControl1.UpdateImageInfo(old_key, new_key);
                    UpdateJsonStr();
                    label4.Text = "tips:更新成功";
                }

                //重置当前选中图片信息
                _cur_Image_ID = (int)image_id;
                _cur_Image_State = image_state;
            }
            catch(Exception err)
            {
                MessageBox.Show(err.Message);
            }
          
        }

        #endregion


        void UpdateImageInfo(ImageKeys oldkey,ImageKeys newkey,string newtext)
        {
            Control[] lvs = this.Controls.Find("lv", true);
            foreach(var lv in lvs)
            {
                foreach(ListViewItem item in ((ListView)lv).Items)
                {   
                    var oldname = string.Format("{0}%{1}",oldkey.Image_ID,oldkey.Image_State);
                    if(item.Name==oldname)
                    {
                        var newname = string.Format("{0}%{1}", newkey.Image_ID, newkey.Image_State);
                        item.Name = newname;
                        item.Text = newtext;
                        return;
                    }
                }
            }
        }


        #region sqlite帮助函数

        /// <summary>
        /// 执行SQL语句，返回影响的记录数
        /// </summary>
        /// <param name="SQLString">SQL语句</param>
        /// <returns>影响的记录数</returns>
        public int ExecuteSql(string SQLString)
        {
            using (SQLiteConnection connection = new SQLiteConnection(_sqlCon))
            {
                using (SQLiteCommand cmd = new SQLiteCommand(SQLString, connection))
                {
                    try
                    {
                        connection.Open();
                        int rows = cmd.ExecuteNonQuery();
                        return rows;
                    }
                    catch (System.Data.SQLite.SQLiteException E)
                    {
                        connection.Close();
                        throw new Exception(E.Message);
                    }
                }
            }
        }
        #endregion

    }
}
