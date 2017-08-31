using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace 消除元素地图配置器
{

    public enum Const
    {
        NoState = 1000,
    }


    //图像数据结构
    public class stImage
    {

        private Int64 id;
        public Int64 ID
        {
            get { return id; }
            set { id = value; }
        }

        private Image image;
        public Image Img
        {
            get { return image; }
            set { image = value; }
        }
        
        private Int64 image_id;
        public Int64 Image_ID
        {
            get { return image_id;}
            set { image_id = value; }
        }

        private Int64 image_state;
        public Int64 Image_State
        {
            get { return image_state; }
            set { image_state = value; }
        }

        private Int64 image_level;
        public Int64 Image_Level
        {
            get { return image_level; }
            set { image_level = value; }
        }

        private string image_name;
        public string Image_Name
        {
            get { return image_name; }
            set { image_name = value; }
        }
    }

    //元素Json属性结构
    public class ImageProperty
    {
        private Int64 image_id;
        private Int64 image_state;
        private int image_index;

        public int Image_Index
        {
            get { return image_index; }
            set { image_index = value; }
        }

        public Int64 Image_ID
        {
            get { return image_id; }
            set { image_id = value; }
        }
       
        public Int64 Image_State
        {
            get { return image_state; }
            set { image_state = value; }
        }
       
    }

    public class stConfig
    {
        private string name;
        public string Name
        {
            get { return name; }
            set { name = value; }
        }
        private string  config;
        public string Config
        {
            get { return config; }
            set { config = value; }
        }
    }

    public class ImageKeys:IComparable
    {
        private Int64 image_id;
        private Int64 image_state;
        public Int64 Image_ID
        {
            get { return image_id; }
            set { image_id = value; }
        }
        public Int64 Image_State
        {
            get { return image_state; }
            set { image_state = value; }
        }
          
        public ImageKeys(Int64 id,Int64 state)
        {
            image_id = id;
            image_state = state;
        }

        public int CompareTo(Object right)
        {
            if (this.image_id == ((ImageKeys)right).Image_ID && this.image_state == ((ImageKeys)right).Image_State)
            {
                return 0;
            }
            return -1;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is ImageKeys))
            {
                return false;
            }
            var p = (ImageKeys)obj;
            return this.image_id == p.Image_ID && this.image_state == p.Image_State;
        }
        public override int GetHashCode()
        {
            return image_id.GetHashCode() + image_state.GetHashCode();
        }
    }
}
