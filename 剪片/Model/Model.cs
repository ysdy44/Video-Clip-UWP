using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using Windows.Foundation;
using Windows.Media.Editing;
using Windows.Media.Playback;
using Windows.UI.Xaml;

namespace 剪片.Model
{
    public  class Model : INotifyPropertyChanged
    {
        //Time：时间线
        public Action<float> function;
        public Stopwatch watch;//秒表




        #region Global：全局


        //选择工具（决定底栏跳转什么页面）
        private int tool;
        public int Tool
        {
            get { return tool; }
            set
            {
                tool = value;
                this.OnPropertyChanged("Tool");
            }
        }



        //媒体播放
        private bool isplay;
        public bool isPlay
        {
            get { return isplay; }
            set
            {
                isplay = value;

                if (MediaPlayer != null)
                {
                    if (value == true)
                    {
                        this.MediaPlayer.Play();
                        this.watch.Start();
                    }
                    else
                    {
                        this.MediaPlayer.Pause();
                        this.watch.Stop();
                    }
                }

                this.OnPropertyChanged("isPlay");
            }
        }



        //Key：快捷键
        private bool isctrl;
        public bool isCtrl
        {
            get { return isctrl; }
            set
            {
                isctrl = value;
                this.OnPropertyChanged("isCtrl");
            }
        }

        #endregion


        #region Meida：媒体


        public Media Drop = null;


        //当前媒体
        private MediaClip current = null;
        public MediaClip Current
        {
            get { return current; }
            set
            {
                current = value;
                this.OnPropertyChanged("Current");

                if (current == null && this.overlayCurrent == null && this.audioCurrent == null) this.isCurrentNull = false;
                else this.isCurrentNull = true;
            }
        }


        //图层索引 
        public int OverlayIndex;

        private MediaOverlay overlayCurrent = null;
        public MediaOverlay OverlayCurrent
        {
            get { return overlayCurrent; }
            set
            {
                overlayCurrent = value;
                this.OnPropertyChanged("OverlayCurrent");
                

                if (current == null && this.overlayCurrent == null&&this.audioCurrent==null) this.isCurrentNull = false;
                else this.isCurrentNull = true;
            }
        }


        //当前媒体
        private BackgroundAudioTrack audioCurrent = null;
        public BackgroundAudioTrack AudioCurrent
        {
            get { return audioCurrent; }
            set
            {
                audioCurrent = value;
                this.OnPropertyChanged("AudioCurrent");


                if (current == null && this.overlayCurrent == null && this.audioCurrent == null) this.isCurrentNull = false;
                else this.isCurrentNull = true;
            }
        }







        //媒体播放器
        public MediaPlayer MediaPlayer = new MediaPlayer();

        //媒体组合
        public MediaComposition MediaComposition = new MediaComposition();

        public List<Clip> Clips = new List<Clip> { };


        #endregion





        #region  Concrol：控件宽高
         

        //Image：画布缩放
        private float scale =2;
        public float Scale
        {
            get { return scale; }
            set
            {
                scale = value;//缩放
                this.Time = this.CanvasToImage(this.MediaPlayer.Position);
                this.OnPropertyChanged("Scale");
            }
        }

    


        //Image：当前时间 
        private float time;
        public float Time
        {
            get { return time; }
            set
            {
                //截取前八位字符串
                time = value;
                this.OnPropertyChanged("Time");
            }
        }
        

        //Screen屏幕层：Point p点
        //
        //Image显示层：float Time点
        //
        //Canvas画布层：TimeSpace postion位置





        //屏幕层与显示层
        public float ImageToScreen(float x)
        {
            //  return x + this.X;
            return x -  this.Time+this.GridWidthHalf;
        }
        public float ScreenToImage(float x)
        {
            //     return x - this.X;
            return x + this.Time - this.GridWidthHalf;
        }



        //屏幕层与显示层
        public TimeSpan ImageToCanvas(float offset)
        {
            return new TimeSpan(0, 0, (int)(offset / this.scale));
        }
        public float CanvasToImage(TimeSpan t)
        {
            return (float)t.TotalSeconds *this.scale;
        }



        //屏幕层与源图层
        public TimeSpan ScreenToCanvas(float x)
        {
            return ImageToCanvas(ScreenToImage(x));
        }

        public float CanvasToScreen(TimeSpan t)
        {
            return ImageToScreen(CanvasToImage(t));
        }


        #endregion


        #region Binding：绑定

        //图层索引是否为空
        private bool iscurrentNull;
        public bool isCurrentNull
        {
            get { return iscurrentNull; }
            set
            {
                iscurrentNull = value;
                this.OnPropertyChanged("isCurrentNull");
            }
        }
        



        //画布刷新（使用方法：Refresh++）
        private int refresh;
        public int Refresh
        {
            get { return refresh; }
            set
            {
                refresh = value;
                this.OnPropertyChanged("Refresh");
            }
        }







        //画布网格宽度
        public float GridWidth = 1024;
        public float GridHeight = 1024;

        public float GridWidthHalf = 512;



        //提示
        private string tip;
        public string Tip
        {
            get { return tip; }
            set
            {
                tip = value;
                this.OnPropertyChanged("Tip");
            }
        }

        private Visibility tipVisibility = Visibility.Collapsed;
        public Visibility TipVisibility
        {
            get { return tipVisibility; }
            set
            {
                tipVisibility = value;
                this.OnPropertyChanged("TipVisibility");
            }
        }


        #endregion
        




        public Model() { }
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
