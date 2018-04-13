using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media.MediaProperties;
using Windows.Media.Transcoding;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;


namespace 剪片.Control
{
    public sealed partial class TranscoderControl2 : UserControl
    {

        CoreDispatcher _dispatcher = Window.Current.Dispatcher;
        CancellationTokenSource _cts;
        string _OutputFileName = "TranscodeSampleOutput";
        MediaEncodingProfile _Profile;
        StorageFile _InputFile = null;
        StorageFile _OutputFile = null;
        MediaTranscoder _Transcoder = new MediaTranscoder();
        string _OutputType = "MP4";
        string _OutputFileExtension = ".mp4";


        #region DependencyProperty：依赖属性


        public Visibility DialogVisibility
        {
            set { SetValue(DialogVisibilityProperty, value); }
        }
        public static readonly DependencyProperty DialogVisibilityProperty =
            DependencyProperty.Register("DialogVisibility", typeof(Visibility), typeof(TranscoderControl2), new PropertyMetadata(0, new PropertyChangedCallback(DialogVisibilityOnChang)));
        private static void DialogVisibilityOnChang(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            TranscoderControl2 Con = (TranscoderControl2)sender;

            if ((Visibility)e.NewValue == Visibility.Visible)
                Con.ShowMethod();
            else if ((Visibility)e.NewValue == Visibility.Collapsed)
                Con.FadeMethod();
        }

        #endregion



        public TranscoderControl2()
        {
            this.InitializeComponent();
            _cts = new CancellationTokenSource();

            //连接界面
            PickFileButton.Click += new RoutedEventHandler(PickFile);
            SetOutputButton.Click += new RoutedEventHandler(PickOutput);
            TargetFormat.SelectionChanged += new SelectionChangedEventHandler(OnTargetFormatChanged);
            Transcode.Click += new RoutedEventHandler(TranscodePreset);
            Back.Click += new RoutedEventHandler(TranscodeBack);
            Cancel.Click += new RoutedEventHandler(TranscodeCancel);
            

            //文件没有被选中,但PickFileButton禁用所有按钮
            DisableButtons();
            SetPickFileButton(true);
            SetOutputFileButton(true);
            SetCancelButton(false);
        }




        public void Dispose()
        {
            _cts.Dispose();
        }



        #region Transcoder：转码


        private async void PickFile(object sender, RoutedEventArgs e)
        {
            FileOpenPicker picker = new FileOpenPicker();
            picker.SuggestedStartLocation = PickerLocationId.VideosLibrary;
            picker.FileTypeFilter.Add(".wmv");
            picker.FileTypeFilter.Add(".mp4");

            StorageFile file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.Read);

                _InputFile = file;

                //启用按钮
                EnableButtons();
            }
        }

        private async void PickOutput(object sender, RoutedEventArgs e)
        {
            FileSavePicker picker = new FileSavePicker();
            picker.SuggestedStartLocation = PickerLocationId.VideosLibrary;
            picker.SuggestedFileName = _OutputFileName;
            picker.FileTypeChoices.Add(_OutputType, new System.Collections.Generic.List<string>() { _OutputFileExtension });

            _OutputFile = await picker.PickSaveFileAsync();

            if (_OutputFile != null)
            {
                SetTranscodeButton(true);
            }
        }



        private void OnTargetFormatChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (TargetFormat.SelectedIndex)
            {
                case 1:
                    _OutputType = "WMV";
                    _OutputFileExtension = ".wmv";
                    EnableNonSquarePARProfiles();
                    break;
                case 2:
                    _OutputType = "AVI";
                    _OutputFileExtension = ".avi";

                    // 禁用NTSC和PAL概要文件不支持非方块像素纵横比的AVI
                    DisableNonSquarePARProfiles();
                    break;
                default:
                    _OutputType = "MP4";
                    _OutputFileExtension = ".mp4";
                    EnableNonSquarePARProfiles();
                    break;
            }
        }



        private async void TranscodePreset(Object sender, RoutedEventArgs e)
        {
            TranscodeStarts();

            try
            {
                if ((_InputFile != null) && (_OutputFile != null))
                {
                    var preparedTranscodeResult = await _Transcoder.PrepareFileTranscodeAsync(_InputFile, _OutputFile, _Profile);

                    if (EnableMrfCrf444.IsChecked.HasValue && (bool)EnableMrfCrf444.IsChecked)
                    {
                        _Transcoder.VideoProcessingAlgorithm = MediaVideoProcessingAlgorithm.MrfCrf444;
                    }
                    else
                    {
                        _Transcoder.VideoProcessingAlgorithm = MediaVideoProcessingAlgorithm.Default;
                    }

                    if (preparedTranscodeResult.CanTranscode)
                    {
                        SetCancelButton(true);
                        Progress<double> progress = new Progress<double>(TranscodeProgress);
                        await preparedTranscodeResult.TranscodeAsync().AsTask(_cts.Token, progress);
                        TranscodeComplete();
                    }
                    else
                    {
                        TranscodeFailure(preparedTranscodeResult.FailureReason);
                    }
                }
            }
            catch (TaskCanceledException)
            {
                App.Tip("");
                TranscodeError("Transcode Canceled");
            }
            catch (Exception exception)
            {
                TranscodeError(exception.Message);
            }
        }

        private void TranscodeBack(Object sender, RoutedEventArgs e)
        { 
            DialogVisibility = Visibility.Collapsed;
        }



        private void GetPresetProfile(ComboBox combobox)
        {
            _Profile = null;
            VideoEncodingQuality videoEncodingProfile = VideoEncodingQuality.Wvga;
            switch (combobox.SelectedIndex)
            {
                case 0:
                    videoEncodingProfile = VideoEncodingQuality.HD1080p;
                    break;
                case 1:
                    videoEncodingProfile = VideoEncodingQuality.HD720p;
                    break;
                case 2:
                    videoEncodingProfile = VideoEncodingQuality.Wvga;
                    break;
                case 3:
                    videoEncodingProfile = VideoEncodingQuality.Ntsc;
                    break;
                case 4:
                    videoEncodingProfile = VideoEncodingQuality.Pal;
                    break;
                case 5:
                    videoEncodingProfile = VideoEncodingQuality.Vga;
                    break;
                case 6:
                    videoEncodingProfile = VideoEncodingQuality.Qvga;
                    break;
            }

            //根据画质选择格式
            switch (_OutputType)
            {
                case "AVI":
                    _Profile = MediaEncodingProfile.CreateAvi(videoEncodingProfile);
                    break;
                case "WMV":
                    _Profile = MediaEncodingProfile.CreateWmv(videoEncodingProfile);
                    break;
                default:
                    _Profile = MediaEncodingProfile.CreateMp4(videoEncodingProfile);
                    break;
            }

            /*
          对于代码转换音频配置文件,创建编码使用这些api的一个概要:
          MediaEncodingProfile.CreateMp3(audioEncodingProfile)
            MediaEncodingProfile.CreateM4a(audioEncodingProfile)
            MediaEncodingProfile.CreateWma(audioEncodingProfile)
            MediaEncodingProfile.CreateWav(audioEncodingProfile)
            audioEncodingProfile是其中一个预设:AudioEncodingQuality。
            AudioEncodingQuality高。
            媒介AudioEncodingQuality.Low
            */

        }


        #endregion


        #region state：转码状态



        private void TranscodeStarts( )
        {
            DisableButtons();
            GetPresetProfile(ProfileSelect);

            //明确的消息
            ProgressText.Text = string.Empty;
            RadialProgressBarControl.Value = 0;

            ProgressShowMethod();


            Transcode.Visibility = Visibility.Collapsed;
            Back.Visibility = Visibility.Collapsed;
        }



        private void TranscodeProgress(double percent)
        {
            MediaEncodingProfile MmediaEncodingProfile = new MediaEncodingProfile();

            //进度条
            ProgressText.Text = percent.ToString().Split('.')[0] + "%";
            RadialProgressBarControl.Value = percent;
        }


        private async void TranscodeComplete()
        {
            App.Tip("Transcode completed.");
            App.Tip("Output (" + _OutputFile.Path + ")");
            IRandomAccessStream stream = await _OutputFile.OpenAsync(FileAccessMode.Read);


            EnableButtons();
            SetCancelButton(false);



            Back.Visibility = Visibility.Visible;
        }


        private async void TranscodeCancel(object sender, RoutedEventArgs e)
        {
            try
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = new CancellationTokenSource();

                if (_OutputFile != null)
                {
                    await _OutputFile.DeleteAsync();
                }
            }
            catch (Exception exception)
            {
                TranscodeError(exception.Message);
            }



            Transcode.Visibility = Visibility.Visible;
            Back.Visibility = Visibility.Visible;
        }

        private async void TranscodeFailure(TranscodeFailureReason reason)
        {
            try
            {
                if (_OutputFile != null)
                {
                    await _OutputFile.DeleteAsync();
                }
            }
            catch (Exception exception)
            {
                TranscodeError(exception.Message);
            }

            switch (reason)
            {
                case TranscodeFailureReason.CodecNotFound:
                    TranscodeError("Codec not found.");
                    break;
                case TranscodeFailureReason.InvalidProfile:
                    TranscodeError("Invalid profile.");
                    break;
                default:
                    TranscodeError("Unknown failure.");
                    break;
            }



            ProgressFadeMethod();


             Transcode.Visibility = Visibility.Visible;
            Back.Visibility = Visibility.Visible;
        }



        private async void TranscodeError(string error)
        {
            await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                App.Tip(error);
            });

            EnableButtons();
            SetCancelButton(false);



            ProgressFadeMethod();


            Transcode.Visibility = Visibility.Visible;
            Back.Visibility = Visibility.Visible;
        }



        #endregion


        #region Method：方法


        private async void SetPickFileButton(bool isEnabled)
        {
            await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                PickFileButton.IsEnabled = isEnabled;
            });
        }

        private async void SetOutputFileButton(bool isEnabled)
        {
            await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                SetOutputButton.IsEnabled = isEnabled;
            });
        }

        private async void SetTranscodeButton(bool isEnabled)
        {
            await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                Transcode.IsEnabled = isEnabled;
            });
        }

        private async void SetCancelButton(bool isEnabled)
        {
            await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                Cancel.IsEnabled = isEnabled;
            });
        }


        private async void EnableButtons()
        {
            await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                PickFileButton.IsEnabled = true;
                SetOutputButton.IsEnabled = true;
                TargetFormat.IsEnabled = true;
                ProfileSelect.IsEnabled = true;
                EnableMrfCrf444.IsEnabled = true;

                // 码按钮的初始状态应禁用,直到一个输出文件集。
                Transcode.IsEnabled = false;
            });
        }


        private async void DisableButtons()
        {
            await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                ProfileSelect.IsEnabled = false;
                Transcode.IsEnabled = false;
                PickFileButton.IsEnabled = false;
                SetOutputButton.IsEnabled = false;
                TargetFormat.IsEnabled = false;
                EnableMrfCrf444.IsEnabled = false;
            });
        }


        private void EnableNonSquarePARProfiles()
        {
            ComboBoxItem_NTSC.IsEnabled = true;
            ComboBoxItem_PAL.IsEnabled = true;
        }


        private void DisableNonSquarePARProfiles()
        {
            ComboBoxItem_NTSC.IsEnabled = false;
            ComboBoxItem_PAL.IsEnabled = false;

            // 确保一个有效的配置文件设置
            if ((ProfileSelect.SelectedIndex == 3) || (ProfileSelect.SelectedIndex == 4))
            {
                ProfileSelect.SelectedIndex = 2;
            }
        }







        #endregion



        

        private void ShowMethod()
        {
            Panel.Visibility = Visibility.Visible;
            PanelShow.Begin();


            Transcode.Visibility = Visibility.Visible;
            Back.Visibility = Visibility.Visible;
        }
        private void FadeMethod()
        {
            PanelFade.Begin();
            Panel.Visibility = Visibility.Collapsed;

            ProgressFadeMethod();


            //文件没有被选中,但PickFileButton禁用所有按钮
            DisableButtons();
            SetPickFileButton(true);
            SetOutputFileButton(true);
            SetCancelButton(false);
        }




        private void ProgressShowMethod()
        {
            ProgressGrid.Visibility = Visibility.Visible;
            MainGrid.Visibility = Visibility.Collapsed;
        }
        private void ProgressFadeMethod()
        {
            ProgressGrid.Visibility = Visibility.Collapsed;
            MainGrid.Visibility = Visibility.Visible;
        }




    }
}
