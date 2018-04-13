using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Media.Editing;
using Windows.UI;

namespace 剪片.Model
{
    public class Setting
    {
        public Color 粉 = Color.FromArgb(255, 227, 132, 178);
        public Color 粉透明 = Color.FromArgb(128, 227, 132, 178);
        public Color 紫 = Color.FromArgb(255, 166, 144, 225);
        public Color 紫透明 = Color.FromArgb(128, 166, 144, 225);
        public Color 蓝 = Color.FromArgb(255, 45, 140, 235);
        public Color 蓝透明 = Color.FromArgb(128, 45, 140, 235);
         public Color 绿 = Color.FromArgb(255, 41, 214, 152);
        public Color 绿透明 = Color.FromArgb(128, 41, 214, 152);
        public Color 黄 = Color.FromArgb(255, 247, 240, 20);
        public Color 黄透明 = Color.FromArgb(128, 247, 240, 20);
        public Color 黑 = Color.FromArgb(255, 0, 0, 0);
        public Color 黑透明 = Color.FromArgb(100, 0, 0, 0);
        public Color 灰 = Color.FromArgb(255, 128, 128, 128);
        public Color 灰透明 = Color.FromArgb(100, 128, 128, 128);


        //Ruler：画标尺线
        public float Space = 60;
        public float OverlaySpace = 40;//标尺刻度空间

        public float RulerSpace = 20;//标尺刻度空间
        public CanvasTextFormat RulerTextFormat = new CanvasTextFormat()//字体格式
        {
            FontSize = 18,
            HorizontalAlignment = CanvasHorizontalAlignment.Center,
            VerticalAlignment = CanvasVerticalAlignment.Top,
        };



    }
}
