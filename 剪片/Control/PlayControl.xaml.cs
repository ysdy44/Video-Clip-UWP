using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.ApplicationModel.Resources;

namespace 剪片.Control
{
    public sealed partial class PlayControl : UserControl
    {

        //Delegate
        public delegate void PlayChangeHandler(object sender, bool isPlay);
        public event PlayChangeHandler PlayChange = null;


        #region DependencyProperty：依赖属性

        public bool isPlay
        {
            get { return (bool)GetValue(isPlayProperty); }
            set { SetValue(isPlayProperty, value); }
        }
        public static readonly DependencyProperty isPlayProperty =
            DependencyProperty.Register("isPlay", typeof(bool), typeof(PlayControl), new PropertyMetadata(false, new PropertyChangedCallback(isPlayOnChang)));

        private static void isPlayOnChang(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            PlayControl Con = (PlayControl)sender;

            if ((bool)e.NewValue == true)
            {
                Con.ToPlay.Begin();//Storyboard
            }
            else
            {
                Con.ToStop.Begin();//Storyboard
            }
        }



        #endregion


        public PlayControl()
        {
            this.InitializeComponent();
        }


        #region Global：全局

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (isPlay == true)
            {
                ToPlay.Begin();//Storyboard
            }
            else
            {
                ToStop.Begin();//Storyboard
            }
        }
        private void PlayButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (isPlay == true)
            {
                isPlay = false;
                PlayChange?.Invoke(this, isPlay);//Delegate
            }
            else
            {
                isPlay = true;
                PlayChange?.Invoke(this, isPlay);//Delegate
            }
        }




        #endregion




        #region Frame：帧进度


        private void StartFrameButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            //起始
            App.Model.MediaPlayer.Position = TimeSpan.Zero;
            App.Model.Time = App.Model.CanvasToImage(App.Model.MediaPlayer.Position);

            App.Model.Refresh++;
        }
        private void LastFrameButton_Tapped(object sender, RoutedEventArgs e)
        {
            //累减
            App.Model.MediaPlayer.Position -= new TimeSpan(0, 0, 1);
            App.Model.Time = App.Model.CanvasToImage(App.Model.MediaPlayer.Position);

            App.Model.Refresh++;
        }
        private void NextFrameButton_Tapped(object sender, RoutedEventArgs e)
        {
            //累加
            App.Model.MediaPlayer.Position += new TimeSpan(0, 0, 1);
            App.Model.Time = App.Model.CanvasToImage(App.Model.MediaPlayer.Position);

            App.Model.Refresh++;
        }
        private void EndFrameButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            App.Model.isPlay = false;

            //结束
            App.Model.MediaPlayer.Position = App.Model.MediaComposition.Duration;
            App.Model.Time = App.Model.CanvasToImage(App.Model.MediaPlayer.Position);

            App.Model.Refresh++;
        }



        #endregion

 
    }
}
