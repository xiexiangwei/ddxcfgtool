using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace 消除元素地图配置器
{
    public partial class MapControl : UserControl
    {
        private ImageList _ImageList;
        private int _ImageIndex;
        private stImage _CurImage;
        //委托
        public delegate void UpdateJsonDlg(string str);
        public UpdateJsonDlg _UpdateJson;

        public delegate stImage GetImageByIDDlg(int image_id, Int64 image_state);
        public GetImageByIDDlg _GetImageByID;

        public delegate void OnClickPBDlg(int x, int y, SortedDictionary<int, ImageProperty> imgmap);
        public OnClickPBDlg _OnClickPB; 

        //9*9二维数组 key:层级
        SortedDictionary<int, ImageProperty>[,] _ImageArray = new SortedDictionary<int, ImageProperty>[9, 9];

        public MapControl()
        {
            InitializeComponent();
            for(int x=0;x<9;x++)
            {
                for(int y=0;y<9;y++)
                {
                    _ImageArray[x, y] = new SortedDictionary<int, ImageProperty>();
                }
            }
        }
       
        private void MapControl_Load(object sender, EventArgs e)
        {
            for(int x=0;x<9;x++)
            {   
                for(int y=0;y<9;y++)
                {
                    System.Windows.Forms.PictureBox p = new System.Windows.Forms.PictureBox();
                    p.AllowDrop = true;
                    p.BackColor = System.Drawing.Color.LightSlateGray;
                    p.Location = new System.Drawing.Point(65*y, 65*x);
                    p.Name = string.Format("{0}:{1}", x, y);
                    p.Size = new System.Drawing.Size(65, 65);
                    p.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Normal;
                    p.Parent = this;
                    p.BorderStyle = BorderStyle.FixedSingle;
                    p.MouseClick += new MouseEventHandler(this.PictureBox_MouseClick);
                }
            }
        }

        public void SetImageList(ImageList imagelist)
        {
            _ImageList = imagelist;
        }

        public void SetImageIndex(int index)
        {
            _ImageIndex = index;
        }

        public void SetImage(stImage img)
        {
            _CurImage = img;
        }
        
        public void UpdateJsonStr()
        {
            _UpdateJson(ImageArray2Json());
        }
        
        private void PictureBox_MouseClick(object sender, MouseEventArgs e)
        {
            PictureBox pb = ((PictureBox)sender);
            string[] pos = pb.Name.Split(':');
            Int64 x = Convert.ToInt64(pos[0]);
            Int64 y = Convert.ToInt64(pos[1]);
            if (_ImageList != null && _ImageIndex < _ImageList.Images.Count)
            {

                if (_ImageIndex >= 0)
                {
                    pb.Image = _ImageList.Images[_ImageIndex];
                    stImage curImage = _GetImageByID(Convert.ToInt32(_CurImage.Image_ID),Convert.ToInt32(_CurImage.Image_State));
                    ImageProperty imgPro = new ImageProperty();
                    imgPro.Image_ID = _CurImage.Image_ID;
                    imgPro.Image_State = curImage.Image_State;
                    imgPro.Image_Index = _ImageIndex;
                    int img_level = Convert.ToInt32(curImage.Image_Level);
                    if (!_ImageArray[x, y].ContainsKey(img_level))
                    {
                        _ImageArray[x, y].Add(img_level, imgPro);
                    }
                    else
                    {
                        _ImageArray[x, y][img_level] = imgPro;
                    }
                    UpdateJsonStr();
                }
                _OnClickPB((int)x, (int)y, _ImageArray[(int)x, (int)y]);
            }
        }

        public void UpdateImageInfo(ImageKeys oldIK, ImageKeys newIK)
        {
            for(int x=0;x<9;x++)
            {
                for(int y=0;y<9;y++)
                {
                    foreach(KeyValuePair<int, ImageProperty> kv in _ImageArray[x,y])
                    {
                        if (kv.Value.Image_ID == oldIK.Image_ID && kv.Value.Image_State == oldIK.Image_State)
                        {
                            _ImageArray[x, y][kv.Key].Image_ID = newIK.Image_ID;
                            _ImageArray[x, y][kv.Key].Image_State = newIK.Image_State;
                        }
                    }
                }
            }
        }

        public string ImageArray2Json(int x_size=9,int y_size=9)
        {
            string text = "\"data\":\r\n[\r\n";
            for (int x = 0; x < x_size; x++)
            {
                text += "[";
                for (int y = 0; y < y_size; y++)
                {   
                    
                    if (_ImageArray[x, y].Keys.Count==0)
                    {
                        text += string.Format("[\"{0}\"]",-1);
                    }
                    else
                    {
                        text += "[";
                        int count = 1;
                        //当前默认转换第一个key到json

                        List<int> sortkeys = new List<int>();
                        foreach (var k in _ImageArray[x, y].Keys)
                        {
                            sortkeys.Add(k);
                        }
                        sortkeys.Sort();


                        foreach (var k in sortkeys)
                        {
                            stImage img = _GetImageByID((int)_ImageArray[x, y][k].Image_ID, (int)_ImageArray[x, y][k].Image_State);
                            if (img.Image_State == Convert.ToInt64(Const.NoState))
                            {
                                text += string.Format("\"{0}\"", img.Image_ID);
                            }
                            else
                            {
                                text += string.Format("\"{0}:{1}\"", img.Image_ID, img.Image_State);
                            }
                            if (count < _ImageArray[x, y].Count)
                            {
                                text += ",";
                            }
                            count++;
                        }
                        text += "]";
                    }
                    if (y != y_size-1)
                    {
                        text += ",";
                    }
                }
                text += "]";
                if (x != x_size-1)
                {
                    text += ",\r\n";
                }
            }
            text += "\r\n]";
            return text;
        }


        public void ResetMap()
        {
            for (int x = 0; x < 9; x++)
            {
                for (int y = 0; y < 9; y++)
                {
                    PictureBox pb = this.Controls.Find(string.Format("{0}:{1}", x, y),true)[0] as PictureBox;
                    pb.Image = null;
                    _ImageArray[x, y] = new SortedDictionary<int, ImageProperty>();

                }
            }
        }

        private void ResetMap2()
        {
            for (int x = 0; x < 9; x++)
            {
                for (int y = 0; y < 9; y++)
                {
                    PictureBox pb = this.Controls.Find(string.Format("{0}:{1}", x, y), true)[0] as PictureBox;
                    pb.Image = null;
                }
            }
        }


       

        public void DeleteImg(string str)
        {
            string[] info = str.Split(':');
            int x = Convert.ToInt32(info[0]);
            int y = Convert.ToInt32(info[1]);
            int image_id = Convert.ToInt32(info[2]);
            int image_level = Convert.ToInt32(info[3]);


            var imgMap = _ImageArray[x, y];
            if (imgMap.ContainsKey(image_level))
            {
                imgMap.Remove(image_level);
                UpdateJsonStr();
                _OnClickPB(x, y, _ImageArray[x,y]);
                //重置地图显示
                PictureBox pb = this.Controls.Find(string.Format("{0}:{1}", x, y), true)[0] as PictureBox;
                if(pb!=null)
                {
                    if (_ImageArray[x, y].Count > 0)
                    {
                        foreach (var k in _ImageArray[x, y].Keys)
                        {
                            pb.Image = _ImageList.Images[_ImageArray[x, y][k].Image_Index];
                            break;
                        }
                    }
                    else
                    {
                        pb.Image = null;
                    }
                }
            }
        }

        public void ShowPicsByConfig(SortedDictionary<int, ImageProperty>[,] cfg)
        {
            for (int x = 0; x < 9; x++)
            {
                for (int y = 0; y < 9; y++)
                {
                    PictureBox pb = this.Controls.Find(string.Format("{0}:{1}", x, y), false)[0] as PictureBox;
                    if(cfg[x, y].Keys.Count > 0)
                    {
                        foreach (var k in cfg[x, y].Keys)
                        {
                            int imageid = (int)cfg[x,y][k].Image_Index;
                            pb.Image = _ImageList.Images[imageid]; 
                            break;
                        }
                    }
                }
            }
        }

        public void LoadConfig(SortedDictionary<int, ImageProperty>[,] cfg)
        {
            _ImageArray = cfg;
            UpdateJsonStr();
            ResetMap2();
            ShowPicsByConfig(cfg);
        }

    }
}
