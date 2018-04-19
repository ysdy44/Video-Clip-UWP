using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.System;
using Windows.ApplicationModel.Resources;
using Windows.Media.Playback;
using System.Threading.Tasks;
using Windows.Storage.AccessCache;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Media.Effects;
using Windows.Media.Devices;
using Windows.Media.Editing;
using Windows.Media.Core;
using Windows.Media.Import;
using Windows.Media.Render;
using Windows.Media.Audio;
using Windows.Media.Transcoding;
using Windows.Media.MediaProperties;
using Windows.UI.Core;
using System.Collections.ObjectModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.FileProperties;
using Windows.UI.Xaml.Media.Imaging;
using 剪片.Model;
using Windows.UI.Xaml.Shapes;
using Windows.Storage.Streams;
using System.Threading;
using Windows.UI.Popups;
using Windows.Media;
using Windows.Media.Capture;

namespace 剪片
{
    public sealed partial class DrawPage : Page
    {
        private StorageItemAccessList storageItemAccessList;

        ObservableCollection<Media> MediaClipList = new ObservableCollection<Media>();


        public DrawPage()
        {
            this.InitializeComponent();
            this.DataContext = App.Model;
        }

                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                     
        #region Global：全局


        protected override void OnNavigatedTo(NavigationEventArgs e)   //当本页面成为框架中的活动页时调用
        {
            //确保我们不会耗尽StoreItemAccessList条目
            //当我们不需要坚持这个跨应用程序会话页
            //每次结算应该足够。
            storageItemAccessList = StorageApplicationPermissions.FutureAccessList;
            storageItemAccessList.Clear();
        }


        protected override void OnNavigatedFrom(NavigationEventArgs e)//当本页面不再是框架中的活动页时调用
        {
            base.OnNavigatedFrom(e); //运行基类的OnNavigatedFrom方法
        }


        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            JudgeWidth(e.NewSize.Width);//Width：屏幕宽度
        }


        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            MediaList.ItemsSource = MediaClipList;

            //Main：主1
             AddColor(Colors.Black, false);
            AddColor(Colors.White, false);

            App.Model.MediaComposition.OverlayLayers.Add(new MediaOverlayLayer());
            App.Model.MediaComposition.OverlayLayers.Add(  new MediaOverlayLayer());


            MediaClip clip = MediaClip.CreateFromColor(Colors.White, new TimeSpan(24, 1, 0));
            clip.TrimTimeFromEnd = clip.TrimTimeFromStart = new TimeSpan(11, 58, 0);
            App.Model.MediaComposition.Clips.Add(clip);
        }


        #endregion

        #region Drag&Drop：拖放


        private void MediaList_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            App.Model.Drop = e.Items.First() as Media;
        }




        private void CanvasControl_DragEnter(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Copy;

            e.DragUIOverride.IsGlyphVisible = false;
            e.DragUIOverride.Caption = "Add";
        }
        private void MainCanvasControl_DragOver(object sender, DragEventArgs e)
        {
        }
        private void CanvasControl_Drop(object sender, DragEventArgs e)
        {
            DragOperationDeferral def = e.GetDeferral();
            def.Complete();

            if (App.Model.Drop.Clip != null)
            {
               Point p = e.GetPosition(CanvasControl);

                //Main：主剪辑
                if (p.Y < App.Setting.Space + App.Setting.RulerSpace)
                {
                    MediaClip clip = App.Model.Drop.Clip.Clone();
                    TimeSpan postion = App.Model.ScreenToCanvas((float)p.X);  //当前时间
                    TimeSpan postiondouble = postion + postion;//双倍当前时间（为了与以后的start+end做比较计算）

                    if (App.Model.MediaComposition.Clips.Count==0)//轨道是空的
                    {
                        App.Model.MediaComposition.Clips.Add( clip);
                    }
                    else if (App.Model.MediaComposition.Clips.Count == 1)//轨道上只有一个
                    {
                        var First = App.Model.MediaComposition.Clips.First();

                        if (postiondouble < First.StartTimeInComposition + First.EndTimeInComposition)
                            App.Model.MediaComposition.Clips.Insert(0, clip);
                        else
                            App.Model.MediaComposition.Clips.Insert(1, clip);
                    }
                    else//轨道上有多个
                    {
                        //判断是否超出第一个剪辑的结束时间 
                        MediaClip First = App.Model.MediaComposition.Clips.FirstOrDefault();
                        if (postiondouble < First.StartTimeInComposition + First.EndTimeInComposition)
                            App.Model.MediaComposition.Clips.Insert(0, clip);

                        //循环，寻找中间落脚点在哪里         
                        TimeSpan OldStartAndEnd = TimeSpan.Zero;
                        for (int i = 1; i < App.Model.MediaComposition.Clips.Count; i++)
                        {
                            MediaClip Current = App.Model.MediaComposition.Clips[i];
                            //是否处于前一个媒体剪辑与后一个媒体剪辑的中点之间
                            if (postiondouble > OldStartAndEnd && postiondouble < Current.StartTimeInComposition + Current.EndTimeInComposition)
                                App.Model.MediaComposition.Clips.Insert(i, clip);

                            OldStartAndEnd = Current.StartTimeInComposition + Current.EndTimeInComposition;//新旧交替
                        }

                        //判断是否超出最后一个剪辑的结束时间
                        MediaClip Last = App.Model.MediaComposition.Clips.LastOrDefault();
                        if (postiondouble > Last.StartTimeInComposition + Last.EndTimeInComposition)
                            App.Model.MediaComposition.Clips.Insert(App.Model.MediaComposition.Clips.Count, clip);

                    }


                    //设此媒体剪辑为当前媒体剪辑当前
                    App.Model.Current = clip;
                    App.Model.OverlayCurrent = null;
                }
                else
                {
                    //OverlayMove：覆盖移动
                    for (int i = 0; i < App.Model.MediaComposition.OverlayLayers.Count; i++)
                    {
                        float top = App.Setting.RulerSpace + App.Setting.Space + i * App.Setting.OverlaySpace;//顶部
                        float height = App.Setting.OverlaySpace;//高度
                        float bottom = top + height;//底部

                        if (p.Y > top && p.Y < bottom)
                        {
                            App.Model.OverlayIndex = i;

                            MediaClip clip = App.Model.Drop.Clip.Clone();
                            MediaOverlay Overlay = new MediaOverlay(clip);
                            Overlay.Position = new Rect(100, 0, 666, 222);

                            App.Model.MediaComposition.OverlayLayers[i].Overlays.Add(Overlay);
                            Overlay.Delay = App.Model.ScreenToCanvas((float)p.X);

                            App.Model.Current = null;
                            App.Model.OverlayCurrent = Overlay;
                            App.Model.AudioCurrent = null;
                         }
                    }
                }

                    App.Model.MediaPlayer.Source = MediaSource.CreateFromMediaStreamSource
                (
                    App.Model.MediaComposition.GeneratePreviewMediaStreamSource(0, 0)
                 );

                this.MediaPlayerElement.SetMediaPlayer(App.Model.MediaPlayer);
                App.Model.Refresh++;//画布刷新


                App.Model.Drop = null;
            }
        }


        #endregion

        #region Width：屏幕宽度


        //屏幕模式枚举
        private enum WidthEnum
        {
            Initialize,//初始状态

            PhoneNarrow,//手机竖屏
            PhoneStrath,//手机横屏

            Pad,//平板横屏
            Pc//电脑
        }
        WidthEnum owe = WidthEnum.Initialize;//OldWidthEnum：旧屏幕宽度枚举
        WidthEnum nwe = WidthEnum.Initialize;//NewWidthEnum：新屏幕宽度枚举


        private void JudgeWidth(double w)
        {
            //根据屏幕宽度判断
            if (w < 600) nwe = WidthEnum.PhoneNarrow;
            else if (w >= 600 && w < 800) nwe = WidthEnum.PhoneStrath;
            else if (w >= 800 && w < 1000) nwe = WidthEnum.Pad;
            else if (w >= 1000) nwe = WidthEnum.Pc;

            if (nwe != owe)//窗口变化过程中，新旧屏幕模式枚举不一样
            {

                //侧栏          
                if (nwe == WidthEnum.PhoneNarrow || nwe == WidthEnum.PhoneStrath)//如果手机竖屏或手机横屏
                {
                    //关闭
                    RightSplitView.DisplayMode = SplitViewDisplayMode.Overlay;
                    RightSplitView.IsPaneOpen = false;

                    //右
                    ZoomSlider.Visibility = Visibility.Collapsed;
                    SplitButton.Visibility = Visibility.Visible;
                }
                else if (nwe == WidthEnum.Pad || nwe == WidthEnum.Pc)//如果平板或电脑
                {
                    //打开
                    RightSplitView.DisplayMode = SplitViewDisplayMode.CompactInline;
                    RightSplitView.IsPaneOpen = true;

                    //右
                    ZoomSlider.Visibility = Visibility.Visible;
                    SplitButton.Visibility = Visibility.Collapsed;
                }


                /*

                //侧栏          
                if (nwe == WidthEnum.PhoneNarrow || nwe == WidthEnum.PhoneStrath ||nwe == WidthEnum.Pad)//如果手机竖屏或手机横屏或平板
                {  
                    //打开
                    LeftSplitView.DisplayMode = SplitViewDisplayMode.Overlay;
                    LeftSplitView.IsPaneOpen = false;
                }
                else if ( nwe == WidthEnum.Pc)//如果电脑
                {  
                    //打开
                    LeftSplitView.DisplayMode = SplitViewDisplayMode.Inline;
                    LeftSplitView.IsPaneOpen = true;
                }

                 */

                owe = nwe;
            }
        }


        #endregion





        #region TrimCopyRemove：复制粘贴移除


        //裁剪
        private void TrimButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            TimeSpan Position = App.Model.MediaPlayer.Position;

            if (App.Model.Current != null)
            {
                //寻找当前媒体剪辑
                int index = App.Model.MediaComposition.Clips.IndexOf(App.Model.Current);
 
                if (Position > App.Model.Current.StartTimeInComposition && Position < App.Model.Current.EndTimeInComposition)
                {
                    //克隆并剪切
                    MediaClip clipA = App.Model.Current.Clone();
                    clipA.TrimTimeFromEnd = (App.Model.Current.EndTimeInComposition + App.Model.Current.TrimTimeFromEnd) - Position;
                    MediaClip clipB = App.Model.Current.Clone();
                    clipB.TrimTimeFromStart = Position - (App.Model.Current.StartTimeInComposition - App.Model.Current.TrimTimeFromStart);

                    //移除并插入
                    App.Model.MediaComposition.Clips.Remove(App.Model.Current);
                    App.Model.MediaComposition.Clips.Insert(index, clipB);
                    App.Model.MediaComposition.Clips.Insert(index, clipA);

                    App.Model.Current = clipB;
                }
            }
            else if (App.Model.OverlayCurrent != null)
            {
                IList<MediaOverlay> overlays = App.Model.MediaComposition.OverlayLayers[App.Model.OverlayIndex].Overlays;

                if (Position > App.Model.OverlayCurrent.Clip.StartTimeInComposition && Position < App.Model.OverlayCurrent.Clip.EndTimeInComposition)
                {
                    //克隆并剪切
                    MediaOverlay overlayA = App.Model.OverlayCurrent.Clone();
                    overlayA.Clip.TrimTimeFromEnd =    (App.Model.OverlayCurrent.Clip.EndTimeInComposition + App.Model.OverlayCurrent.Clip.TrimTimeFromEnd) - Position;
                    MediaOverlay overlayB = App.Model.OverlayCurrent.Clone();
                    overlayB.Clip.TrimTimeFromStart = Position - (App.Model.OverlayCurrent.Clip.StartTimeInComposition - App.Model.OverlayCurrent.Clip.TrimTimeFromStart);
                    overlayB.Delay = Position;

                    //移除并插入
                    overlays.Remove(App.Model.OverlayCurrent);
                    overlays.Add(overlayA);
                    overlays.Add(overlayB);

                    App.Model.OverlayCurrent = overlayB;
                 }
            }
            else if (App.Model.AudioCurrent != null)
            {
                if (Position > App.Model.AudioCurrent.Delay && Position < App.Model.AudioCurrent.Delay + App.Model.AudioCurrent.TrimmedDuration)
                {
                    //克隆并剪切
                    BackgroundAudioTrack audioA = App.Model.AudioCurrent.Clone();
                    audioA.TrimTimeFromEnd = (App.Model.AudioCurrent.Delay+App.Model.AudioCurrent.TrimmedDuration + App.Model.AudioCurrent.TrimTimeFromEnd) - Position;
                    BackgroundAudioTrack audioB = App.Model.AudioCurrent.Clone();
                    audioB.TrimTimeFromStart = Position - (App.Model.AudioCurrent.Delay - App.Model.AudioCurrent.TrimTimeFromStart);
                    audioB.Delay = Position;

                    //移除并插入
                    App.Model.MediaComposition.BackgroundAudioTracks.Remove(App.Model.AudioCurrent);
                    App.Model.MediaComposition.BackgroundAudioTracks.Add(audioA);
                    App.Model.MediaComposition.BackgroundAudioTracks.Add(audioB);

                    App.Model.AudioCurrent = audioB;
                }
            }
        }

        //拷贝
        private void CopyButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (App.Model.Current != null)
            {
                //克隆
                MediaClip clip = App.Model.Current.Clone();

                //插入
                int index = App.Model.MediaComposition.Clips.IndexOf(App.Model.Current);
                App.Model.MediaComposition.Clips.Insert(index, clip);

                //更新当前
                App.Model.Current = clip;
            }
            else if (App.Model.OverlayCurrent != null)
            {
                IList<MediaOverlay> overlays = App.Model.MediaComposition.OverlayLayers[App.Model.OverlayIndex].Overlays;

                //克隆
                MediaOverlay overlay = App.Model.OverlayCurrent.Clone();
                overlay.Delay = App.Model.OverlayCurrent.Clip.EndTimeInComposition;

                //插入
                int index = overlays.IndexOf(App.Model.OverlayCurrent);
                overlays.Insert(index, overlay);

                //更新当前
                App.Model.OverlayCurrent = overlay;
            }
            else if (App.Model.AudioCurrent != null)
            {
                BackgroundAudioTrack Audio = App.Model.AudioCurrent.Clone();

                App.Model.MediaComposition.BackgroundAudioTracks.Add(Audio);
                Audio.Delay = App.Model.AudioCurrent.Delay + App.Model.AudioCurrent.TrimmedDuration;

                App.Model.AudioCurrent = Audio;
            }

            App.Model.Refresh++;//画布刷新
        }

        //移除
        private void RemoveButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (App.Model.Current != null)
            {
                App.Model.MediaComposition.Clips.Remove(App.Model.Current);
                App.Model.Current = null;
            }
            else if (App.Model.OverlayCurrent != null)
            {
                IList<MediaOverlay> overlays = App.Model.MediaComposition.OverlayLayers[App.Model.OverlayIndex].Overlays;

                overlays.Remove(App.Model.OverlayCurrent);
                App.Model.OverlayCurrent = null;
            }
            else if (App.Model.AudioCurrent != null)
            {
                 App.Model.MediaComposition.BackgroundAudioTracks.Remove(App.Model.AudioCurrent);
                App.Model.AudioCurrent = null;
            }

            App.Model.Refresh++;//画布刷新
        }



        #endregion

        #region Add：添加


        private async void AddVideoButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            FileOpenPicker filePicker = new FileOpenPicker { SuggestedStartLocation = PickerLocationId.Desktop };
            filePicker.FileTypeFilter.Add(".m4v");
            filePicker.FileTypeFilter.Add(".mp4");//可以
            filePicker.FileTypeFilter.Add(".mov");//可以
            filePicker.FileTypeFilter.Add(".asf");
            filePicker.FileTypeFilter.Add(".avi");//？？？
            filePicker.FileTypeFilter.Add(".wmv");//微软的官方格式，可以
            filePicker.FileTypeFilter.Add(".m2ts");
            filePicker.FileTypeFilter.Add(".3g2");
            filePicker.FileTypeFilter.Add(".3gp2");
            filePicker.FileTypeFilter.Add(".3gpp");

            filePicker.FileTypeFilter.Add(".flv");//？？？

            filePicker.ViewMode = PickerViewMode.Thumbnail;
            StorageFile file = await filePicker.PickSingleFileAsync();

            if (file != null)
            {
                try
                {
                    StorageItemAccessList storageItemAccessList = StorageApplicationPermissions.FutureAccessList;
                    storageItemAccessList.Add(file);


                    //新建媒体
                    Media media = new Media();

                    //缩略图
                    using (StorageItemThumbnail thumbnail = await file.GetThumbnailAsync(ThumbnailMode.VideosView))
                    {
                        if (thumbnail != null)
                        {
                            BitmapImage bitmapImage = new BitmapImage();
                            bitmapImage.SetSource(thumbnail);

                            media.Bitmap = bitmapImage;
                        }
                    }

                    //从文件创建媒体剪辑
                    MediaClip clip = await MediaClip.CreateFromFileAsync(file);
                    media.Clip = clip;

                    //从颜色创建名称
                    media.Name = file.Name;

                    MediaClipList.Add(media);
                    SplitShowMethod();// SplitView：侧栏
                }
                catch (Exception)
                {
                    App.Tip("file err");
                }
            }
            else App.Tip("file null");
        }


        private async void AddMusicButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            FileOpenPicker filePicker = new FileOpenPicker { SuggestedStartLocation = PickerLocationId.MusicLibrary };
            filePicker.FileTypeFilter.Add(".mp3");
            filePicker.FileTypeFilter.Add(".wma");
            filePicker.FileTypeFilter.Add(".wav");

            filePicker.FileTypeFilter.Add(".asf");//微软的格式
            filePicker.FileTypeFilter.Add(".ogg");//开源的格式
            filePicker.FileTypeFilter.Add(".flac");//开源的格式
            filePicker.FileTypeFilter.Add(".aac");

            filePicker.ViewMode = PickerViewMode.Thumbnail;
            StorageFile file = await filePicker.PickSingleFileAsync();

            if (file != null)
            {
                try
                {
                    StorageItemAccessList storageItemAccessList = StorageApplicationPermissions.FutureAccessList;
                    storageItemAccessList.Add(file);


                    //新建媒体
                    Media media = new Media();

                    //缩略图
                    using (StorageItemThumbnail thumbnail = await file.GetThumbnailAsync(ThumbnailMode.MusicView))
                    {
                        if (thumbnail != null)
                        {
                            BitmapImage bitmapImage = new BitmapImage();
                            bitmapImage.SetSource(thumbnail);

                            media.Bitmap = bitmapImage;
                        }
                    }

                    //从文件创建媒体剪辑
                    MediaClip clip = await MediaClip.CreateFromFileAsync(file);
                    media.Clip = clip;

                    //从颜色创建名称
                    media.Name = file.Name;

                    MediaClipList.Add(media);
                    SplitShowMethod();// SplitView：侧栏
                }
                catch (Exception)
                {
                    App.Tip("file err");
                }
            }
            else App.Tip("file null");
        }


        private async void AddPicturesButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            FileOpenPicker filePicker = new FileOpenPicker { SuggestedStartLocation = PickerLocationId.PicturesLibrary };
            filePicker.FileTypeFilter.Add(".jpg");
            filePicker.FileTypeFilter.Add(".jpeg");
            filePicker.FileTypeFilter.Add(".png");
            filePicker.FileTypeFilter.Add(".bmp");
            filePicker.FileTypeFilter.Add(".gif");
            filePicker.FileTypeFilter.Add(".tiff");
            filePicker.ViewMode = PickerViewMode.Thumbnail;
            StorageFile file = await filePicker.PickSingleFileAsync();

            if (file != null)
            {
                try
                {
                    StorageItemAccessList storageItemAccessList = StorageApplicationPermissions.FutureAccessList;
                    storageItemAccessList.Add(file);


                    //新建媒体
                    Media media = new Media();

                    //缩略图
                    using (StorageItemThumbnail thumbnail = await file.GetThumbnailAsync(ThumbnailMode.PicturesView))
                    {
                        if (thumbnail != null)
                        {
                            BitmapImage bitmapImage = new BitmapImage();
                            bitmapImage.SetSource(thumbnail);

                            media.Bitmap = bitmapImage;
                        }
                    }

                    //从文件创建媒体剪辑
                    MediaClip clip = await MediaClip.CreateFromImageFileAsync(file, new TimeSpan(24, 1, 0));
                    clip.TrimTimeFromEnd = clip.TrimTimeFromStart = new TimeSpan(12, 0, 0);
                    media.Clip = clip;

                    //从颜色创建名称
                    media.Name = file.Name;

                    MediaClipList.Add(media);
                    SplitShowMethod();// SplitView：侧栏
                }
                catch (Exception)
                {
                    App.Tip("file err");
                }
            }
            else App.Tip("file null");
        }



        private void AddColorWhiteButton_Tapped(object sender, TappedRoutedEventArgs e) { AddColor(Colors.White); }
        private void AddColorBlackButton_Tapped(object sender, TappedRoutedEventArgs e) { AddColor(Colors.Black); }
        private void AddColorRedButton_Tapped(object sender, TappedRoutedEventArgs e) { AddColor(Colors.Red); }
        private void AddColorGreenButton_Tapped(object sender, TappedRoutedEventArgs e) { AddColor(Colors.Green); }
        private void AddColorBlueButton_Tapped(object sender, TappedRoutedEventArgs e) { AddColor(Colors.Blue); }

        private void AddColor(Color co,bool isAnumal=true)  //从颜色创建媒体剪辑
        {
            //新建媒体
            Media media = new Media();

            //从颜色创建剪辑
            MediaClip clip = MediaClip.CreateFromColor(co, new TimeSpan(24, 1, 0));
            clip.TrimTimeFromEnd = clip.TrimTimeFromStart = new TimeSpan(12, 0, 0);
            media.Clip = clip;

            //从颜色创建笔刷
            media.Brush = new SolidColorBrush(co);

            //从颜色创建名称
            media.Name = "#" + 剪片.Library.Method.ColorToString(co);

            MediaClipList.Add(media);
            if (isAnumal)   SplitShowMethod();// SplitView：侧栏
        }


        #endregion

        #region Property：属性


        private void PropertyFlyout_Opened(object sender, object e)
        {
            if (App.Model.Current != null)
            {
                PropertyMediaGrid.Visibility = Visibility.Visible;

                PropertyMediaVolumeSlider.Value = App.Model.Current.Volume;
            }
            else PropertyMediaGrid.Visibility = Visibility.Collapsed;



            if (App.Model.OverlayCurrent != null)
            {
                PropertyOverlayGrid.Visibility = Visibility.Visible;
                
                PropertyOverlayXNumberPicker.Value = (int)App.Model.OverlayCurrent.Position.X;
                PropertyOverlayYNumberPicker.Value = (int)App.Model.OverlayCurrent.Position.Y;
                PropertyOverlayWNumberPicker.Value = (int)App.Model.OverlayCurrent.Position.Width;
                PropertyOverlayHNumberPicker.Value = (int)App.Model.OverlayCurrent.Position.Height;

                OverlayAudioSlider.IsChecked = App.Model.OverlayCurrent.AudioEnabled;
                OverlayVolumeSlider.Value = App.Model.OverlayCurrent.Clip.Volume;
                OverlayOpacitySlider.Value = App.Model.OverlayCurrent.Opacity;

                OverlayUpButton.IsEnabled = App.Model.OverlayIndex > 0;//判断上升按钮是否可用
                OverlayDownButton.IsEnabled = App.Model.OverlayIndex < App.Model.MediaComposition.OverlayLayers.Count - 1;//判断下降按钮是否可用      
            }
            else PropertyOverlayGrid.Visibility = Visibility.Collapsed;



            if (App.Model.AudioCurrent != null)
            {
                PropertyAudioGrid.Visibility = Visibility.Visible;

                PropertyAudioVolumeSlider.Value = App.Model.AudioCurrent.Volume;
            }
            else PropertyAudioGrid.Visibility = Visibility.Collapsed;



            if (App.Model.Current == null&&App.Model.OverlayCurrent == null&&App.Model.AudioCurrent == null)
            {
                PropertyNullGrid.Visibility = Visibility.Visible;
            }
            else PropertyNullGrid.Visibility = Visibility.Collapsed;
        }



        private void PropertyFlyout_Closed(object sender, object e)
        {
            if (App.Model.Current != null)
            {
                if (App.Model.MediaPlayer.Position > App.Model.Current.StartTimeInComposition && App.Model.MediaPlayer.Position < App.Model.Current.EndTimeInComposition)
                {
                    //时间回到媒体剪辑播放前，这样才能改变声音
                    App.Model.MediaPlayer.Position = App.Model.Current.StartTimeInComposition;
                    App.Model.Time = App.Model.CanvasToImage(App.Model.MediaPlayer.Position);
                }
            }
            else if (App.Model.OverlayCurrent != null)
            {
                if (App.Model.MediaPlayer.Position>App.Model.OverlayCurrent.Clip.StartTimeInComposition&& App.Model.MediaPlayer.Position <App.Model.OverlayCurrent.Clip.EndTimeInComposition)
                {
                    //时间回到媒体剪辑播放前，这样才能改变声音
                    App.Model.MediaPlayer.Position = App.Model.OverlayCurrent.Clip.StartTimeInComposition;
                    App.Model.Time = App.Model.CanvasToImage(App.Model.MediaPlayer.Position);
                }
            }
            else if (App.Model.AudioCurrent != null)
            {
                if (App.Model.MediaPlayer.Position > App.Model.AudioCurrent.Delay && App.Model.MediaPlayer.Position < App.Model.AudioCurrent.Delay+ App.Model.AudioCurrent.TrimmedDuration)
                {
                     //时间回到媒体剪辑播放前，这样才能改变声音
                    App.Model.MediaPlayer.Position = App.Model.AudioCurrent.Delay;
                    App.Model.Time = App.Model.CanvasToImage(App.Model.MediaPlayer.Position);
                }
            }
        }







        private void PropertyMediaVolumeSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e){ if (App.Model.Current != null) App.Model.Current.Volume = e.NewValue; }







        private void PropertyXNumberPicker_ValueChange(object sender, int Value){if (App.Model.OverlayCurrent != null)App.Model.OverlayCurrent.Position = new Rect(Value,App.Model.OverlayCurrent.Position.Y,App.Model.OverlayCurrent.Position.Width,App.Model.OverlayCurrent.Position.Height);}
        private void PropertyYNumberPicker_ValueChange(object sender, int Value){if (App.Model.OverlayCurrent != null)App.Model.OverlayCurrent.Position = new Rect(App.Model.OverlayCurrent.Position.X,Value,App.Model.OverlayCurrent.Position.Width,App.Model.OverlayCurrent.Position.Height);}
        private void PropertyWNumberPicker_ValueChange(object sender, int Value){if (App.Model.OverlayCurrent != null)App.Model.OverlayCurrent.Position = new Rect(App.Model.OverlayCurrent.Position.X,App.Model.OverlayCurrent.Position.Y,Value,App.Model.OverlayCurrent.Position.Height);}
        private void PropertyHNumberPicker_ValueChange(object sender, int Value){if (App.Model.OverlayCurrent != null)App.Model.OverlayCurrent.Position = new Rect(App.Model.OverlayCurrent.Position.X,App.Model.OverlayCurrent.Position.Y,App.Model.OverlayCurrent.Position.Width,Value);}


        private void OverlayAudioSlider_Checked(object sender, RoutedEventArgs e){if (App.Model.OverlayCurrent != null)App.Model.OverlayCurrent.AudioEnabled = true;}
        private void OverlayAudioSlider_Unchecked(object sender, RoutedEventArgs e){if (App.Model.OverlayCurrent != null)App.Model.OverlayCurrent.AudioEnabled = false;}

        private void OverlayVolumeSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e){ if (App.Model.OverlayCurrent != null) App.Model.OverlayCurrent.Clip.Volume = e.NewValue; }
        private void OverlayOpacitySlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e){ if (App.Model.OverlayCurrent != null) App.Model.OverlayCurrent.Opacity = e.NewValue; }


        private void OverlayUpButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (App.Model.OverlayCurrent != null)
            {
                if (App.Model.OverlayIndex > 0)
                {
                    //从本层移除
                    App.Model.MediaComposition.OverlayLayers[App.Model.OverlayIndex].Overlays.Remove(App.Model.OverlayCurrent);

                    //加入上层
                    App.Model.OverlayIndex--;
                    App.Model.MediaComposition.OverlayLayers[App.Model.OverlayIndex].Overlays.Add(App.Model.OverlayCurrent);
                }

                OverlayUpButton.IsEnabled = App.Model.OverlayIndex > 0;//判断上升按钮是否可用
                OverlayDownButton.IsEnabled = App.Model.OverlayIndex < App.Model.MediaComposition.OverlayLayers.Count - 1;//判断下降按钮是否可用      
             }
        }
        private void OverlayDownButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (App.Model.OverlayCurrent!=null)
            {
                if (App.Model.OverlayIndex < App.Model.MediaComposition.OverlayLayers.Count - 1)
                {
                    //从本层移除
                    App.Model.MediaComposition.OverlayLayers[App.Model.OverlayIndex].Overlays.Remove(App.Model.OverlayCurrent);

                    //加入上层
                    App.Model.OverlayIndex++;
                    App.Model.MediaComposition.OverlayLayers[App.Model.OverlayIndex].Overlays.Add(App.Model.OverlayCurrent);
                }

                OverlayUpButton.IsEnabled = App.Model.OverlayIndex > 0;//判断上升按钮是否可用
                OverlayDownButton.IsEnabled = App.Model.OverlayIndex < App.Model.MediaComposition.OverlayLayers.Count - 1;//判断下降按钮是否可用      
            }
        }




            private void PropertyAudioVolumeSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e) { if (App.Model.AudioCurrent != null) App.Model.AudioCurrent.Volume = e.NewValue; }




        #endregion

        #region Other：其他


        //覆盖层
        private void LayerButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            //新建媒体覆盖层
            MediaOverlayLayer OverlayLayer = new MediaOverlayLayer();

            App.Model.MediaComposition.OverlayLayers.Add(OverlayLayer);
        }

        private async void EffectButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            //VideoStabilizationEffectDefinition vsed = new VideoStabilizationEffectDefinition();
            //VideoEffectDefinition ved = new VideoEffectDefinition(vsed.ActivatableClassId, vsed.Properties);

             // 创建一个属性集,并添加一个属性/值对
            PropertySet echoProperties = new PropertySet();
            echoProperties.Add("Mix", 0.5f);

             PropertySet properties = new PropertySet();
            properties.Add("FadeValue", 5);

            VideoTransformEffectDefinition van = new VideoTransformEffectDefinition();
            VideoEffectDefinition effect = new VideoEffectDefinition(van.ActivatableClassId, van.Properties);
            
            //   VideoEffectDefinition effect = new VideoEffectDefinition(typeof(ExampleVideoEffect).FullName);

            if (App.Model.Current != null)
            {
                try
                {
                    App.Model.Current.VideoEffectDefinitions.Add(effect);
                }
                catch (Exception exception)
                {
                    await new MessageDialog(exception.Message).ShowAsync();
                }
            }
            else if (App.Model.OverlayCurrent != null)
            {
                try
                {
                    App.Model.OverlayCurrent.Clip.VideoEffectDefinitions.Add(new VideoEffectDefinition(typeof(ExampleVideoEffect).FullName));
                }
                catch (Exception exception)
                {
                    await new MessageDialog(exception.Message).ShowAsync();
                }
            }
        }

        private async void AudioButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            await AddBackgroundAudioTrack();
        }





        private async Task AddBackgroundAudioTrack()
        {
            var picker = new FileOpenPicker();
            picker.SuggestedStartLocation = PickerLocationId.MusicLibrary;
            picker.FileTypeFilter.Add(".mp3");
            picker.FileTypeFilter.Add(".wav");
            picker.FileTypeFilter.Add(".flac");
            StorageFile audioFile = await picker.PickSingleFileAsync();
            if (audioFile != null)
            {

                StorageItemAccessList storageItemAccessList = StorageApplicationPermissions.FutureAccessList;
                storageItemAccessList.Add(audioFile);

                BackgroundAudioTrack backgroundTrack = await BackgroundAudioTrack.CreateFromFileAsync(audioFile);
                App.Model.MediaComposition.BackgroundAudioTracks.Add(backgroundTrack);

                //裁剪，防止播出
                if (backgroundTrack.TrimmedDuration>App.Model.MediaComposition.Duration)
                {
                    backgroundTrack.Delay = TimeSpan.Zero;
                    backgroundTrack.TrimTimeFromEnd = App.Model.MediaComposition.Duration - backgroundTrack.TrimmedDuration;
                 }
            }
            else
            {
                App.Tip("File picking cancelled");
            }
        }


        #endregion





        #region SaveOpen：保存打开


        private void HomeButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            LeftSplitView.IsPaneOpen = false;
        }
        private void NewButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            App.Model.MediaComposition = new MediaComposition();

            this.MediaPlayerElement.SetMediaPlayer(App.Model.MediaPlayer);
            App.Model.Refresh++;//画布刷新
        }
        private async void ExportButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            await RenderComposition();
        }
        private async void SaveButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            await SaveComposition();
        }
        private async void SaveAdButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            await SaveComposition();
        }
        private async void OpenButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            await OpenComposition();

            App.Model.MediaPlayer.Source = MediaSource.CreateFromMediaStreamSource
            (
                App.Model.MediaComposition.GeneratePreviewMediaStreamSource(0, 0)
             );
            this.MediaPlayerElement.SetMediaPlayer(App.Model.MediaPlayer);
        }




        private void TranscoderButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            LeftSplitView.IsPaneOpen = false;

            TranscoderControl.DialogVisibility = Visibility.Visible;
        }






        //渲染视频文件
        private async Task RenderComposition()
        {
            var picker = new FileSavePicker();
            picker.SuggestedStartLocation = PickerLocationId.VideosLibrary;
            picker.FileTypeChoices.Add("MP4 files", new List<string>() { ".mp4" });
            picker.SuggestedFileName = "TrimmedClip.mp4";

            StorageFile file = await picker.PickSaveFileAsync();
            if (file != null)
            {
                var saveOperation = App.Model.MediaComposition.RenderToFileAsync(file, MediaTrimmingPreference.Precise);

                //输出进度
                App.Model.TipVisibility = Visibility.Visible;
                App.Model.Tip = string.Format("Saving file... Progress: 0%");

                saveOperation.Progress = new AsyncOperationProgressHandler<TranscodeFailureReason, double>(async (info, progress) =>
                {
                    await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler(() =>
                    {
                        //输出进度
                        App.Model.Tip = string.Format("Saving file... Progress: {0:F0}%", progress);
                    }));
                });

                saveOperation.Completed = new AsyncOperationWithProgressCompletedHandler<TranscodeFailureReason, double>(async (info, status) =>
                {
                    await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler(() =>
                    {
                        try
                        {
                            var results = info.GetResults();
                            if (results != TranscodeFailureReason.None || status != AsyncStatus.Completed)
                            {
                                //保存失败
                                App.Tip("Saving was unsuccessful");

                            }
                            else
                            {
                                //保存成功
                                 App.Tip("Trimmed clip saved to file");
                             }
                        }
                        finally
                        {
                            //输出进度
                            App.Model.TipVisibility = Visibility.Collapsed;
                        }
                    }));
                });
            }
            else
            {
                //用户取消了文件的选择
                App.Tip("User cancelled the file selection");
             }
        } 


        //将组合保存到文件中
        //媒体组合可以序列化为要在以后修改的文件。选择一个输出文件, 然后调用 MediaComposition 方法 SaveAsync 以保存组合。
        private async Task SaveComposition()
        {
            var picker = new FileSavePicker();
            picker.SuggestedStartLocation = PickerLocationId.VideosLibrary;
            picker.FileTypeChoices.Add("Composition files", new List<string>() { ".cmp" });
            picker.SuggestedFileName = "SavedComposition";

            StorageFile compositionFile = await picker.PickSaveFileAsync();
            if (compositionFile != null)
            {
                var action = App.Model.MediaComposition.SaveAsync(compositionFile);
                action.Completed = (info, status) =>
                {
                    if (status != AsyncStatus.Completed)
                    {
                        //存储错误
                        App.Tip("a]save err");
                    }
                };
            }
        }


        //从文件加载组合
        //可以从文件中反序列化媒体组合, 以允许用户查看和修改组合。选择一个组合文件, 然后调用 MediaComposition 方法 LoadAsync 以加载该组合。
        private async Task OpenComposition()
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.VideosLibrary;
            picker.FileTypeFilter.Add(".cmp");

            StorageFile compositionFile = await picker.PickSingleFileAsync();
            if (compositionFile == null)
            {
                //w文件选择失败
            }
            else
            {
                App.Model.MediaComposition = await MediaComposition.LoadAsync(compositionFile);

                if (App.Model.MediaComposition != null)
                {

                }
                else
                {
                    //无法打开组合
                }
            }
        }


        #endregion

        #region SplitView：侧栏


        private void MenuButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            LeftSplitView.IsPaneOpen = !LeftSplitView.IsPaneOpen; 
        } 

        

        private void SplitShowMethod()
        {
            if (SplitButton.Visibility==Visibility.Visible)
            {
                SplitShow.Begin();
            }
        }

        private void SplitButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            RightSplitView.IsPaneOpen = true;
        }









        #endregion


        private async void MediaList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Media m = MediaList.SelectedItem as Media;
            
            RightText.Text = GetProperties(m);
        }
        private string GetProperties(Media m)
        {
            string s = string.Empty;

            if (m != null)
            {
                try
                {
                    VideoEncodingProperties p = m.Clip.GetVideoEncodingProperties();

                    s =
                          "  " + "Subtype" + "：" + p.Subtype
                          +
                           "  " + "Type" + "：" + p.Type
                          +
                           "  " + "Width" + "：" + p.Width.ToString()
                          +
                           "  " + "Height" + "：" + p.Height.ToString()
                          +
                            "  " + "ProfileId" + "：" + p.ProfileId.ToString();

                }
                catch (Exception exception)
                {
                    new MessageDialog(exception.Message).ShowAsync();
                }
            }
            return s;
        }


    }
}
