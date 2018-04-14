using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Diagnostics;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Editing;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Windows.Media.Effects;
using 剪片.Library;
using 剪片.Model;
using Windows.ApplicationModel.DataTransfer;
using Windows.Media.Core;

namespace 剪片.Control
{ 
    public sealed partial class MainCanvasControl : UserControl
    {

        #region DependencyProperty：依赖属性


        //刷新
        public int Refresh
        {
            get { return (int)GetValue(RefreshProperty); }
            set { SetValue(RefreshProperty, value); }
        }

        public static readonly DependencyProperty RefreshProperty =
            DependencyProperty.Register("Refresh", typeof(int), typeof(MainCanvasControl), new PropertyMetadata(0, new PropertyChangedCallback(RefreshOnChang)));

        private static void RefreshOnChang(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            MainCanvasControl Con = (MainCanvasControl)sender;

            Con.CanvasControl.Invalidate();//刷新画布内容 
        }


        #endregion
        
        public MainCanvasControl()
        {
            this.InitializeComponent();
        }


        #region Global：全局


        private void CanvasSizeChanged(object sender, SizeChangedEventArgs e)
        {
            App.Model.GridWidth = (float)e.NewSize.Width;
            App.Model.GridHeight = (float)e.NewSize.Height;

            App.Model.GridWidthHalf = App.Model.GridWidth / 2;

        }


        #endregion




        #region Virtual & Animated：虚拟 & 动画


        private void CanvasControl_CreateResources(CanvasControl sender, CanvasCreateResourcesEventArgs args)
        {
            App.Model.watch = Stopwatch.StartNew();

            App.Model.function = elapsedTime =>
            {
                if (isTime == false && App.Model.isPlay == true)
                {
                    //Time：时间线 
                    App.Model.Time = App.Model.CanvasToImage(App.Model.MediaPlayer.Position);
                }
            };
        }
        private void CanvasControl_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            RulerDraw(args.DrawingSession);
            MainDraw(args.DrawingSession);
            OverlayDraw(args.DrawingSession);
            if (App.Model.Current != null) MoveDraw(args.DrawingSession);
            if (App.Model.OverlayCurrent != null) OverlayMoveDraw(args.DrawingSession);
            if (App.Model.AudioCurrent != null) AudioMoveDraw(args.DrawingSession);

            TimeDraw(args.DrawingSession);

            // 动画,然后显示当前图像的效果。
            App.Model.function((float)App.Model.watch.Elapsed.TotalSeconds);

            sender.Invalidate();
        }


        #endregion

        #region Second ：第二界面


        //Ruler：画标尺线
        private void RulerDraw(CanvasDrawingSession ds)
        {
            float zero = App.Model.ImageToScreen(0);//零线
            float Duration = App.Model.CanvasToScreen(App.Model.MediaComposition.Duration);//终线

            //间隔
            float space = (10 * App.Model.Scale);
            while (space < 10) space *= 5; //大则小
            while (space > 100) space /= 5;//小则大
            float spaceFive = space * 5;//五倍


            //水平虚线循环
            for (float X = zero; X < Duration; X += space) ds.DrawLine(X, 0, X, App.Setting.RulerSpace, App.Setting.灰透明);
            //水平实线循环
            for (float X = zero; X < Duration; X += spaceFive) ds.DrawLine(X, 0, X, App.Setting.RulerSpace, App.Setting.灰);


            //零线
            ds.DrawLine(Duration, 0, Duration, App.Model.GridHeight, App.Setting.灰透明);
            //终线
            ds.DrawLine(zero, 0, zero, App.Model.GridHeight, App.Setting.灰透明);


            //标尺线
            ds.DrawLine(0, App.Setting.RulerSpace, App.Model.GridWidth, App.Setting.RulerSpace, App.Setting.灰透明);
          
        }


        //Main：主渲染
        private void MainDraw(CanvasDrawingSession ds)
        {
            float top = App.Setting.RulerSpace;//顶部
            float height = App.Setting.Space;//高度
            float bottom = top + height;//底部
            float center = top + height / 2;//中间
            ds.FillRectangle(0, top, App.Model.GridWidth, height, App.Setting.黑透明);//Main

            //Main：剪辑遍历
            foreach (var clip in App.Model.MediaComposition.Clips)//主媒体剪辑循环
            {
                float left = App.Model.CanvasToScreen(clip.StartTimeInComposition);//左边
                float right = App.Model.CanvasToScreen(clip.EndTimeInComposition);//右边
                float width = right - left;

                ds.FillRoundedRectangle(left, top, width, height, 4, 4, App.Setting.紫透明);
                ds.DrawRoundedRectangle(left, top, width, height, 4, 4, App.Setting.紫, 2);
            }

            AudioDraw(ds);
        }


        private void AudioDraw(CanvasDrawingSession ds)
        {
            float top = 0;//顶部
            float height = App.Setting.RulerSpace;//高度
            float bottom = top + height;//底部
            float center = top + height / 2;//中间

            foreach (var Audio in App.Model.MediaComposition.BackgroundAudioTracks)//主媒体剪辑循环
            {
                float left = App.Model.CanvasToScreen(Audio.Delay);//左边
                float right = App.Model.CanvasToScreen(Audio.Delay+ Audio.TrimmedDuration);//右边
                float width = right - left;

                ds.FillRoundedRectangle(left, top, width, height, 4, 4, App.Setting.粉透明);
                ds.DrawRoundedRectangle(left, top, width, height, 4, 4, App.Setting.粉, 2);
            }
        }

        //Overlay：覆盖渲染
        private void OverlayDraw(CanvasDrawingSession ds)
        {
            //Overlay：覆盖层遍历
            for (int i = 0; i < App.Model.MediaComposition.OverlayLayers.Count; i++)//上下层循环
            {
                float top = App.Setting.RulerSpace + App.Setting.Space + i * App.Setting.OverlaySpace;//顶部
                float height = App.Setting.OverlaySpace;//高度
                float bottom = top + height;//底部
                float center = top + height / 2;//中间

                //上下分割线
                 ds.DrawLine(0, bottom, App.Model.GridWidth, bottom, App.Setting.灰透明);

                //覆盖遍历
                foreach (var Overlays in App.Model.MediaComposition.OverlayLayers[i].Overlays)//层的左右媒体剪辑循环
                {
                    float left = App.Model.CanvasToScreen(Overlays.Clip.StartTimeInComposition);//左边
                    float right = App.Model.CanvasToScreen(Overlays.Clip.EndTimeInComposition);//右边
                    float width = right - left;

                    ds.FillRoundedRectangle(left, top, width, height, 4, 4, App.Setting.蓝透明);
                    ds.DrawRoundedRectangle(left, top, width, height, 4, 4, App.Setting.蓝, 2);
                }
            }
        }



        //Move：移动     //Trim：裁切
        private void MoveDraw(CanvasDrawingSession ds)
        {
            float top = App.Setting.RulerSpace;//顶部
            float height = App.Setting.Space;//高度
            float bottom = top + height;//底部
            float center = top + height / 2;//中间

            //Trim：寻找当前媒体剪辑（如果处于Trim，就用Trim的数据，否则就用当前媒体剪辑的数据）
            float left = isTrimStart ?TrimStartInX :App.Model.CanvasToScreen(App.Model.Current.StartTimeInComposition);//左边
            float right = isTrimEnd ?TrimEndInX :App.Model.CanvasToScreen(App.Model.Current.EndTimeInComposition);//右边
            float width = right - left;



          
            //Move：移动
            if (isMove == true && MoveX != MoveStartX)//如果移动了，就绘制移动光标矩形
            {
                //Move：绘制当前黄色矩形
                ds.DrawRectangle(left, top, width, height, App.Setting.黄透明, 2);  


                //移动索引位置边线
                ds.DrawLine(MovePostion, top, MovePostion, bottom, App.Setting.黄, 4); 

                //改变数据
                left = MoveX - 10;
                right = MoveX + 10;
                width = 20;

                //绘制移动光标矩形
                ds.FillRoundedRectangle(MoveX - 10, top, 20, height, 4, 4, App.Setting.黄透明);     //填充
                ds.DrawRoundedRectangle(MoveX - 10, top, 20, height, 4, 4, App.Setting.黄, 2);//描边
            }
            else
            {
                //Move：绘制当前黄色矩形
                ds.DrawRectangle(left, top, width, height, App.Setting.黄, 2);
            }


            //Trim：裁切
            if (isTrimStart) //绘制左时间文字
            {
                //黄线
                ds.DrawLine(TrimStartInX, 0, TrimStartInX, App.Model.GridHeight, App.Setting.黑透明, 4);
                ds.DrawLine(TrimStartInX, 0, TrimStartInX, App.Model.GridHeight, App.Setting.绿, 2);

                ds.FillRoundedRectangle(TrimStartInX- 40, 0, 80, 20, 2, 2, App.Setting.绿);//填充圆角矩形
                ds.FillCircle(TrimStartInX, 20, 5, App.Setting.绿);//填充圆
                //绘制文字
                 string s = (TrimStartInTime - App.Model.Current.StartTimeInComposition).ToString().Substring(0, 8);
                ds.DrawText(s, TrimStartInX, -4, Colors.White, App.Setting.RulerTextFormat);
            }
            else //绘制左黄点
            {
                ds.FillCircle(left, center, 12, App.Setting.黑透明);
                ds.FillCircle(left, center, 10, App.Setting.黄);
                ds.FillCircle(left, center, 5, Colors.White);
            }


            if (isTrimEnd) //绘制右时间文字
            {
                //黄线
                ds.DrawLine(TrimEndInX, 0, TrimEndInX, App.Model.GridHeight, App.Setting.黑透明, 4);
                ds.DrawLine(TrimEndInX, 0, TrimEndInX, App.Model.GridHeight, App.Setting.绿, 2);

                ds.FillRectangle(TrimEndInX - 40, 0, 80, 20,App.Setting.绿);//填充圆角矩形
                ds.FillCircle(TrimEndInX, 20, 5, App.Setting.绿);//填充圆
                //绘制文字
                string s = (TrimEndInTime-App.Model.Current.EndTimeInComposition).ToString().Substring(0, 8);
                ds.DrawText(s, TrimEndInX, -4, Colors.White, App.Setting.RulerTextFormat);
            }
            else//绘制右黄点
            {
                ds.FillCircle(right, center, 12, App.Setting.黑透明);
                ds.FillCircle(right, center, 10, App.Setting.黄);
                ds.FillCircle(right, center, 5, Colors.White);
            } 
        }

        //OverlayMove：覆盖移动     //OverlayTrim：覆盖裁切
        private void OverlayMoveDraw(CanvasDrawingSession ds)
        {
            float top = App.Setting.OverlaySpace * App.Model.OverlayIndex + App.Setting.Space + App.Setting.RulerSpace;//顶部
            float height = App.Setting.OverlaySpace;//高度
            float bottom = top + height;//底部
            float center = top + height / 2;//中间

            //Trim：寻找当前媒体剪辑（如果处于Trim，就用Trim的数据，否则就用当前媒体剪辑的数据）
            float left = isOverlayTrimStart ? OverlayTrimStartInX : App.Model.CanvasToScreen(App.Model.OverlayCurrent.Clip.StartTimeInComposition);//左边
            float right = isOverlayTrimEnd ? OverlayTrimEndInX : App.Model.CanvasToScreen(App.Model.OverlayCurrent.Clip.EndTimeInComposition);//左边
             float width = right - left;


            //绘制当前黄色矩形
            ds.DrawRectangle(left, top, width, height, App.Setting.黄, 2);
           
             //Trim：裁切
            if (isOverlayTrimStart) //绘制左时间文字
            {
                //黄线
                ds.DrawLine(OverlayTrimStartInX, 0, OverlayTrimStartInX, App.Model.GridHeight, App.Setting.黑透明, 4);
                ds.DrawLine(OverlayTrimStartInX, 0, OverlayTrimStartInX, App.Model.GridHeight, App.Setting.绿, 2);

                ds.FillRoundedRectangle(OverlayTrimStartInX - 40, 0, 80, 20, 2, 2, App.Setting.绿);//填充圆角矩形
                ds.FillCircle(OverlayTrimStartInX, 20, 5, App.Setting.绿);//填充圆
                //绘制文字
                 string s = (OverlayTrimStartInTime - App.Model.OverlayCurrent.Clip.StartTimeInComposition).ToString().Substring(0, 8);
                ds.DrawText(s, OverlayTrimStartInX, -4, Colors.White, App.Setting.RulerTextFormat);
            }
            else //绘制左黄点
            {
                ds.FillCircle(left, center, 12, App.Setting.黑透明);
                ds.FillCircle(left, center, 10, App.Setting.黄);
                ds.FillCircle(left, center, 5, Colors.White);
            }


            if (isOverlayTrimEnd) //绘制右时间文字
            {
                //黄线
                ds.DrawLine(OverlayTrimEndInX, 0, OverlayTrimEndInX, App.Model.GridHeight, App.Setting.黑透明, 4);
                ds.DrawLine(OverlayTrimEndInX, 0, OverlayTrimEndInX, App.Model.GridHeight, App.Setting.绿, 2);

                ds.FillRoundedRectangle(OverlayTrimEndInX - 40, 0, 80, 20, 2, 2, App.Setting.绿);//填充圆角矩形
                ds.FillCircle(OverlayTrimEndInX, 20, 5, App.Setting.绿);//填充圆
                //绘制文字
                 string s = (OverlayTrimEndInTime - App.Model.OverlayCurrent.Clip.EndTimeInComposition).ToString().Substring(0, 8);
                 ds.DrawText(s, OverlayTrimEndInX, -4, Colors.White, App.Setting.RulerTextFormat);
            }
            else//绘制右黄点
            {
                ds.FillCircle(right, center, 12, App.Setting.黑透明);
                ds.FillCircle(right, center, 10, App.Setting.黄);
                ds.FillCircle(right, center, 5, Colors.White);
            }
        }

        //AudioMove：覆盖移动     //AudioTrim：覆盖裁切
        private void AudioMoveDraw(CanvasDrawingSession ds)
        {
            float top = 0;//顶部
            float height = App.Setting.RulerSpace;//高度
            float bottom = top + height;//底部
            float center = top + height / 2;//中间

            //Trim：寻找当前媒体剪辑（如果处于Trim，就用Trim的数据，否则就用当前媒体剪辑的数据）
            float left = isAudioTrimStart ? AudioTrimStartInX : App.Model.CanvasToScreen(App.Model.AudioCurrent.Delay);//左边
            float right = isAudioTrimEnd ? AudioTrimEndInX : App.Model.CanvasToScreen(App.Model.AudioCurrent.Delay+ App.Model.AudioCurrent.TrimmedDuration);//左边
            float width = right - left;


            //绘制当前黄色矩形
            ds.DrawRectangle(left, top, width, height, App.Setting.黄, 2);

            //Trim：裁切
            if (isAudioTrimStart) //绘制左时间文字
            {
                //黄线
                ds.DrawLine(AudioTrimStartInX, 0, AudioTrimStartInX, App.Model.GridHeight, App.Setting.黑透明, 4);
                ds.DrawLine(AudioTrimStartInX, 0, AudioTrimStartInX, App.Model.GridHeight, App.Setting.绿, 2);

                ds.FillRoundedRectangle(AudioTrimStartInX - 40, 0, 80, 20, 2, 2, App.Setting.绿);//填充圆角矩形
                ds.FillCircle(AudioTrimStartInX, 20, 5, App.Setting.绿);//填充圆
                //绘制文字
                 string s = (AudioTrimStartInTime-App.Model.AudioCurrent.Delay).ToString().Substring(0, 8);
                ds.DrawText(s, AudioTrimStartInX, -4, Colors.White, App.Setting.RulerTextFormat);
            }
            else //绘制左黄点
            {
                ds.FillCircle(left, center, 12, App.Setting.黑透明);
                ds.FillCircle(left, center, 10, App.Setting.黄);
                ds.FillCircle(left, center, 5, Colors.White);
            }


            if (isAudioTrimEnd) //绘制右时间文字
            {
                //黄线
                ds.DrawLine(AudioTrimEndInX, 0, AudioTrimEndInX, App.Model.GridHeight, App.Setting.黑透明, 4);
                ds.DrawLine(AudioTrimEndInX, 0, AudioTrimEndInX, App.Model.GridHeight, App.Setting.绿, 2);

                ds.FillRoundedRectangle(AudioTrimEndInX - 40, 0, 80, 20, 2, 2, App.Setting.绿);//填充圆角矩形
                ds.FillCircle(AudioTrimEndInX, 20, 5, App.Setting.绿);//填充圆
                //绘制文字
                 string s = (AudioTrimEndInTime - (App.Model.AudioCurrent.Delay+App.Model.AudioCurrent.TrimmedDuration)).ToString().Substring(0, 8);
                ds.DrawText(s, AudioTrimEndInX, -4, Colors.White, App.Setting.RulerTextFormat);
            }
            else//绘制右黄点
            {
                ds.FillCircle(right, center, 12, App.Setting.黑透明);
                ds.FillCircle(right, center, 10, App.Setting.黄);
                ds.FillCircle(right, center, 5, Colors.White);
            }
        }


        //Time：时间线
        private void TimeDraw(CanvasDrawingSession ds)
        {
            //红线
            ds.DrawLine(App.Model.GridWidthHalf, 0, App.Model.GridWidthHalf, App.Model.GridHeight, App.Setting.黑透明, 4);
            ds.DrawLine(App.Model.GridWidthHalf, 0, App.Model.GridWidthHalf, App.Model.GridHeight, App.Setting.蓝, 2);

            //绘制时间与圆角矩形
            if (isTime == true)
            {
                ds.FillRoundedRectangle(App.Model.GridWidthHalf - 40, 0, 80, 20, 2, 2, App.Setting.蓝);//填充圆角矩形
                ds.FillCircle(App.Model.GridWidthHalf, 20, 5, App.Setting.蓝);//填充圆
                //绘制文字
                string s = App.Model.ImageToCanvas(App.Model.Time).ToString().Substring(0, 8);
                ds.DrawText(s, App.Model.GridWidthHalf, -4, Colors.White, App.Setting.RulerTextFormat);
            }
        }
        

        #endregion





        #region Point：指针事件


        float ManipulationStart;//初始控制尺寸
        float ManipulationStartScale;//初始全局尺寸
        Point ManipulationStartPoint;//初始控制点

        private void CanvasManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            //记录初始位置
            ManipulationStart = e.Cumulative.Scale;
            ManipulationStartScale = App.Model.Scale;

            App.Model.Refresh++; //画布刷新
        }
        private void CanvasManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            float offset = e.Cumulative.Scale / ManipulationStart;//尺寸累乘
            if (offset != 1)
            {
                App.Model.Scale = ManipulationStartScale * offset;//计算全局尺寸
            }
            else
            {
                Single_Delta(new Point(ManipulationStartPoint.X + e.Cumulative.Translation.X, ManipulationStartPoint.Y + e.Cumulative.Translation.Y));
            }

            App.Model.Refresh++; //画布刷新
        }
        private void CanvasManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            float offset = e.Cumulative.Scale / ManipulationStart;//尺寸累乘
            if (offset != 1)
            {
                App.Model.Scale = ManipulationStartScale * offset;//计算全局尺寸
            }
            else
            {
                Single_Complete(e.Position);
            }

            App.Model.Refresh++; //画布刷新
        }









        private void CanvasPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            ManipulationStartPoint = Judge.Position(e, CanvasControl);
            Single_Start(ManipulationStartPoint);

            App.Model.isCtrl = true;
        }
        private void CanvasPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            Point p = Judge.Position(e, CanvasControl);
            Single_Complete(p);

            App.Model.isCtrl = false;
        }
        private void CanvasPointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            float Delta = (float)Judge.WheelDelta(e, CanvasControl);

            //Key：快捷键
            if (App.Model.isCtrl)
            {
                if (Delta > 0) App.Model.Scale *= 1.1f;
                else App.Model.Scale /= 1.1f;
            }
            else
            {
                //时间累加
                App.Model.MediaPlayer.Position += App.Model.ImageToCanvas(Delta);
                App.Model.Time = App.Model.CanvasToImage(App.Model.MediaPlayer.Position);
            }

            App.Model.Refresh++;//画布刷新
        }


        #endregion

        #region Event：封装事件


        bool isTime;

        bool isMove;
        bool isTrimStart;
        bool isTrimEnd;

        bool isOverlayMove;
        bool isOverlayTrimStart;
        bool isOverlayTrimEnd;

        bool isAudioMove;
        bool isAudioTrimStart;
        bool isAudioTrimEnd;

        

        //单指&&左键&&笔
        private void Single_Start(Point p)
        {
            TimeSpan CurrentTime = App.Model.ScreenToCanvas((float)p.X);


            //Audio：背景音频
            if (p.Y < App.Setting.RulerSpace)
            {
                // AudioTrim：裁切
                if (App.Model.AudioCurrent != null)
                {
                    double left = App.Model.CanvasToScreen(App.Model.AudioCurrent.Delay);
                    double right = App.Model.CanvasToScreen(App.Model.AudioCurrent.Delay+App.Model.AudioCurrent.TrimmedDuration);

                    if (Math.Abs(p.X - right) < 12)
                    {
                        // AudioTrim：右裁切
                        AudioTrimEnd_Start(p); isAudioTrimEnd = true; return;
                    }
                    if (Math.Abs(p.X - left) < 12)
                    {
                        // AudioTrim：左裁切
                        AudioTrimStart_Start(p); isAudioTrimStart = true; return;
                    }
                }


                //AudioMove：移动
                App.Model.AudioCurrent = App.Model.MediaComposition.BackgroundAudioTracks.FirstOrDefault(   // 返回序列中满足条件的第一个元素；如果未找到这样的元素，则返回类的默认值。
                    mc =>
                    mc.Delay <= CurrentTime &&
                    mc.Delay + mc.TrimmedDuration >= CurrentTime);

                if (App.Model.AudioCurrent != null)
                {
                    TimeSpan LeftTime = App.Model.AudioCurrent.Delay;
                    TimeSpan RightTime = App.Model.AudioCurrent.Delay+App.Model.AudioCurrent.TrimmedDuration;

                    App.Model.Current = null;//清空
                    App.Model.OverlayCurrent = null;//清空
                    App.Model.Tool = 3;

                    if (CurrentTime > LeftTime && CurrentTime < RightTime)
                    {
                        // AudioMove：移动
                       AudioMove_Start(p); isAudioMove = true; return;
                    }
                }
            }


            //Clips：主媒体剪辑
            if (p.Y < App.Setting.RulerSpace + App.Setting.Space)
                {
                    // Trim：裁切
                    if (App.Model.Current != null)
                    {
                        double left = App.Model.CanvasToScreen(App.Model.Current.StartTimeInComposition);
                        double right = App.Model.CanvasToScreen(App.Model.Current.EndTimeInComposition);

                        if (Math.Abs(p.X - right) < 12)
                        {
                            // Trim：右裁切
                            TrimEnd_Start(p); isTrimEnd = true; return;
                        }
                        if (Math.Abs(p.X - left) < 12)
                        {
                            // Trim：左裁切
                            TrimStart_Start(p); isTrimStart = true; return;
                        }
                    }


                    //Move：移动
                    App.Model.Current = App.Model.MediaComposition.Clips.FirstOrDefault(   // 返回序列中满足条件的第一个元素；如果未找到这样的元素，则返回类的默认值。
                        mc =>
                        mc.StartTimeInComposition <= CurrentTime &&
                        mc.EndTimeInComposition >= CurrentTime);

                    if (App.Model.Current != null)
                    {
                        TimeSpan LeftTime = App.Model.Current.StartTimeInComposition;
                        TimeSpan RightTime = App.Model.Current.EndTimeInComposition;

                    App.Model.OverlayCurrent = null;//清空
                    App.Model.AudioCurrent = null;//清空
                    App.Model.Tool = 1;

                    if (CurrentTime > LeftTime && CurrentTime < RightTime)
                        {
                            //Move：移动
                            Move_Start(p); isMove = true; return;
                        }
                    }
                }


            //Overlay：覆盖层
            if (p.Y > App.Setting.RulerSpace + App.Setting.Space)
                {
                    // OverlayTrim：覆盖裁切
                    if (App.Model.OverlayCurrent != null)
                    {
                        float top = App.Setting.RulerSpace + App.Setting.Space + App.Model.OverlayIndex * App.Setting.OverlaySpace;//顶部
                        float height = App.Setting.OverlaySpace;//高度
                        float bottom = top + height;//底部

                        if (p.Y > top && p.Y < bottom)
                        {
                            double left = App.Model.CanvasToScreen(App.Model.OverlayCurrent.Clip.StartTimeInComposition);
                            double right = App.Model.CanvasToScreen(App.Model.OverlayCurrent.Clip.EndTimeInComposition);

                            if (Math.Abs(p.X - left) < 12)
                            {
                                // OverlayTrim：覆盖左裁切
                                OverlayTrimStart_Start(p); isOverlayTrimStart = true; return;
                            }
                            if (Math.Abs(p.X - right) < 12)
                            {
                                // OverlayTrim：覆盖右裁切
                                OverlayTrimEnd_Start(p); isOverlayTrimEnd = true; return;
                            }
                        }
                    }


                    //OverlayMove：覆盖移动
                    for (int i = 0; i < App.Model.MediaComposition.OverlayLayers.Count; i++)
                    {
                        float top = App.Setting.RulerSpace + App.Setting.Space + i * App.Setting.OverlaySpace;//顶部
                        float height = App.Setting.OverlaySpace;//高度
                        float bottom = top + height;//底部

                        if (p.Y > top && p.Y < bottom)
                        {
                            App.Model.OverlayIndex = i;//确认无误后，指定覆盖层索引
                            App.Model.OverlayCurrent = App.Model.MediaComposition.OverlayLayers[i].Overlays.FirstOrDefault( // 返回序列中满足条件的第一个元素；如果未找到这样的元素，则返回类的默认值。
                                mc =>
                                mc.Clip.StartTimeInComposition <= CurrentTime &&
                                mc.Clip.EndTimeInComposition >= CurrentTime);

                            if (App.Model.OverlayCurrent != null)
                            {
                            TimeSpan LeftTime = App.Model.OverlayCurrent.Clip.StartTimeInComposition;
                                TimeSpan RightTime = App.Model.OverlayCurrent.Clip.EndTimeInComposition;

                            App.Model.Current = null;//清空
                            App.Model.AudioCurrent = null;//清空
                            App.Model.Tool = 2;

                            if (CurrentTime > LeftTime && CurrentTime < RightTime)
                                {
                                    //OverlayMove：覆盖移动
                                    OverlayMove_Start(p); isOverlayMove = true; return;
                                }
                            }
                            break;
                        }
                    }
                }
        

            //Time：时间线
            Time_Start(p); isTime = true; return;
        }
        private void Single_Delta(Point p)
        {
            if (isTime == true)
            {
                Time_Delta(p);return;
            }


            if (isMove == true)
            {
                Move_Delta(p);return;
            }
            if (isTrimStart == true)
            {
                TrimStart_Delta(p);return;
            }
            if (isTrimEnd == true)
            {
                TrimEnd_Delta(p);return;
            }


            if (isOverlayMove == true)
            {
                OverlayMove_Delta(p); return;
            }
            if (isOverlayTrimStart == true)
            {
                OverlayTrimStart_Delta(p); return;
            }
            if (isOverlayTrimEnd == true)
            {
                OverlayTrimEnd_Delta(p); return;
            }


            if (isAudioMove == true)
            {
                AudioMove_Delta(p); return;
            }
            if (isAudioTrimStart == true)
            {
                AudioTrimStart_Delta(p); return;
            }
            if (isAudioTrimEnd == true)
            {
                AudioTrimEnd_Delta(p); return;
            }
        }

        private void Single_Complete(Point p)
        {
            if (isTime == true)
            {
                Time_Complete(p);isTime = false;return;
            }


            if (isMove == true)
            {
                Move_Complete(p);isMove = false;return;
            }
            if (isTrimStart == true)
            {
                TrimStart_Complete(p);isTrimStart = false;return;
            }
            if (isTrimEnd == true)
            {
                TrimEnd_Complete(p);isTrimEnd = false;return;
            }


            if (isOverlayMove == true)
            {
                OverlayMove_Complete(p); isOverlayMove = false; return;
            }
            if (isOverlayTrimStart == true)
            {
                OverlayTrimStart_Complete(p); isOverlayTrimStart = false; return;
            }
            if (isOverlayTrimEnd == true)
            {
                OverlayTrimEnd_Complete(p); isOverlayTrimEnd = false; return;
            }


            if (isAudioMove == true)
            {
                AudioMove_Complete(p); isAudioMove = false; return;
            }
            if (isAudioTrimStart == true)
            {
                AudioTrimStart_Complete(p); isAudioTrimStart = false; return;
            }
            if (isAudioTrimEnd == true)
            {
                AudioTrimEnd_Complete(p); isAudioTrimEnd = false; return;
            }
        }


        #endregion




        #region Time：时间轴


        float TimeStartX;//初始X位置
        float TimeStartTime;//初始全局时间

        bool TimeisPlay;//初始是否播放，用于改变时间开始的时候暂停播放，结束后恢复播放

        private void Time_Start(Point p)
        {
            //记录初始位置
            TimeStartX = (float)p.X;
            TimeStartTime = App.Model.Time;

            if (App.Model.isPlay == true)
            {
                TimeisPlay = true;
                App.Model.MediaPlayer.Pause();//暂停    
            }
            else TimeisPlay = false;
        }
        private void Time_Delta(Point p)
        {
            if (App.Model.MediaPlayer != null)
            {
                App.Model.Time = TimeStartTime + TimeStartX - (float)p.X;
                if (App.Model.Time < 0) App.Model.Time = 0;
                App.Model.MediaPlayer.Position = App.Model.ImageToCanvas(App.Model.Time);
            }
        }
        private void Time_Complete(Point p)
        {
            //延续操作之前的播放状态
            if (TimeisPlay == true)
            {
                App.Model.MediaPlayer.Play();//暂停    
                TimeisPlay = false;
            }
        }


        #endregion


        #region Move：移动


        float MoveStartX;//移动起始点
        float MoveX;//移动点（也用于渲染0
        float MovePostion;//移动索引位置（也用于渲染）

        private void Move_Start(Point p)
        {
            MoveStartX= MoveX=(float) p.X;
        }
        private void Move_Delta(Point p)
        {
            MoveX = (float)p.X;

            if (App.Model.Current != null)
            {
                MovePostion = MoveJudgePostion(MoveX);//判断移动到哪一个媒体剪辑之间的位置
            }
        }
        private void Move_Complete(Point p)
        {
            MoveX = (float)p.X;

            if (App.Model.Current != null )
            {
                int index = MoveJudgeIndex(MoveX);//判断移动到哪一个媒体剪辑之间的索引
                if (index >=0&&index<=App.Model.MediaComposition.Clips.Count)
                {
                    //插入并返回（不返回会顺序出错以至于产生bug）
                    MediaClip clip = App.Model.Current.Clone();
                    App.Model.MediaComposition.Clips.Insert(index, clip);
                    App.Model.MediaComposition.Clips.Remove(App.Model.Current);
                    App.Model.Current = clip;
                }
            }
         }




        private float MoveJudgePostion(float x)//判断移动到哪一个媒体剪辑之间的位置
        {
            //Source：数据源
            TimeSpan postion = App.Model.ScreenToCanvas(x);//当前位置的对应的时间
            TimeSpan postiondouble = postion + postion;//双倍当前时间（为了与以后的start+end做比较计算）

            //判断当前时间是否超过自己的起始与结束的时间
            if (postion < App.Model.Current.StartTimeInComposition || postion > App.Model.Current.EndTimeInComposition)
            {
                //判断是否超出第一个剪辑的结束时间
                MediaClip First = App.Model.MediaComposition.Clips.First();
                if (postiondouble < First.StartTimeInComposition + First.EndTimeInComposition)
                    return App.Model.ImageToScreen(0);

                //循环，寻找中间落脚点在哪里         
                TimeSpan OldStartAndEnd = TimeSpan.Zero;
                foreach (var Current in App.Model.MediaComposition.Clips)
                {
                    //是否处于前一个媒体剪辑与后一个媒体剪辑的中点之间
                    if (postiondouble > OldStartAndEnd && postiondouble < Current.StartTimeInComposition + Current.EndTimeInComposition)
                        return App.Model.CanvasToScreen(Current.StartTimeInComposition);

                    OldStartAndEnd = Current.StartTimeInComposition + Current.EndTimeInComposition;//新旧交替
                }

                //判断是否超出最后一个剪辑的结束时间
                MediaClip Last = App.Model.MediaComposition.Clips.Last();
                if (postiondouble > Last.StartTimeInComposition + Last.EndTimeInComposition)
                    return App.Model.CanvasToScreen(   App.Model.MediaComposition.Duration);
            }
            return -10;
        }

        private  int MoveJudgeIndex(float x)//判断移动到哪一个媒体剪辑之间的索引
        {
            //Source：数据源
            TimeSpan postion = App.Model.ScreenToCanvas(x);  //当前时间

            TimeSpan postiondouble = postion + postion;//双倍当前时间（为了与以后的start+end做比较计算）

            //判断当前时间是否超过自己的起始与结束的时间
            if (postion < App.Model.Current.StartTimeInComposition || postion > App.Model.Current.EndTimeInComposition)
            {
                //判断是否超出第一个剪辑的结束时间
                MediaClip First = App.Model.MediaComposition.Clips.First();
                if (postiondouble < First.StartTimeInComposition + First.EndTimeInComposition)
                     return 0;

                //循环，寻找中间落脚点在哪里         
                TimeSpan OldStartAndEnd = TimeSpan.Zero;
                for (int i = 1; i < App.Model.MediaComposition.Clips.Count; i++)
                {
                    MediaClip Current = App.Model.MediaComposition.Clips[i];
                    //是否处于前一个媒体剪辑与后一个媒体剪辑的中点之间
                    if (postiondouble > OldStartAndEnd && postiondouble < Current.StartTimeInComposition + Current.EndTimeInComposition)
                        return i;

                    OldStartAndEnd = Current.StartTimeInComposition + Current.EndTimeInComposition;//新旧交替
                }

                //判断是否超出最后一个剪辑的结束时间
                MediaClip Last = App.Model.MediaComposition.Clips.Last();
                if (postiondouble > Last.StartTimeInComposition + Last.EndTimeInComposition)
                    return App.Model.MediaComposition.Clips.Count;
            }

            return -1;
        }


        #endregion

        #region TrimStart：裁切


        float TrimStartInX;//裁切开始位置，用于画布渲染
        TimeSpan TrimStartInTime;//裁切开始时间，用于画布渲染

        private void TrimStart_Start(Point p)
        {
            TrimStartInX = App.Model.CanvasToScreen(App.Model.Current.StartTimeInComposition);   //先从当前的媒体剪辑，获取裁切开始位置
        }
        private void TrimStart_Delta(Point p)
        {
            TrimStartInTime = TrimStartScreentoCanvas((float)p.X);//位置转时间

            //时间跨度不超出限制
            if (TrimStartInTime < TimeSpan.Zero)
                TrimStartInTime = TimeSpan.Zero;
            else if (TrimStartInTime > App.Model.Current.OriginalDuration - App.Model.Current.TrimTimeFromEnd)
                TrimStartInTime = App.Model.Current.OriginalDuration - App.Model.Current.TrimTimeFromEnd;

            TrimStartInX = TrimStartCanvastoScreen(TrimStartInTime);//时间转位置
        }
        private void TrimStart_Complete(Point p)
        {
            TrimStart_Delta(p);

            App.Model.Current.TrimTimeFromStart = TrimStartInTime;//裁切
        }




        private TimeSpan TrimStartScreentoCanvas(float X)
        {
            return App.Model.Current.TrimTimeFromStart + (App.Model.ScreenToCanvas(X) - App.Model.Current.StartTimeInComposition);
        }
        private float TrimStartCanvastoScreen(TimeSpan time)
        {
            return App.Model.CanvasToScreen(time - (App.Model.Current.TrimTimeFromStart - App.Model.Current.StartTimeInComposition));
        }


        #endregion

        #region TrimEnd：裁切


        float TrimEndInX;//裁切结束位置，用于画布渲染
        TimeSpan TrimEndInTime;//裁切结束时间，用于画布渲染
        
        //TrimEnd：选出最大的时间
        TimeSpan TrimEndOverlayMaxTime;//覆盖层最大时间
        float TrimEndOverlayMaxX;//覆盖层最大位置

        private void TrimEnd_Start(Point p)
        {
            TrimEndInX = App.Model.CanvasToScreen(App.Model.Current.EndTimeInComposition);   //先从当前的媒体剪辑，获取裁切结束位置
             
            //TrimEnd：选出最大的时间
            TrimEndOverlayMaxTime = TimeSpan.Zero;
            foreach (var OverlayLayers in App.Model.MediaComposition.OverlayLayers)
            {
                foreach (var Overlays in OverlayLayers.Overlays)
                {
                    if (TrimEndOverlayMaxTime < Overlays.Clip.StartTimeInComposition) TrimEndOverlayMaxTime = Overlays.Clip.StartTimeInComposition;
                }
            }

            TrimEndOverlayMaxX = App.Model.CanvasToScreen(TrimEndOverlayMaxTime);
        }
        private void TrimEnd_Delta(Point p)
        {
            //TrimEnd：不越过最大的时间
            if (App.Model.Current == App.Model.MediaComposition.Clips.Last() && (float)p.X < TrimEndOverlayMaxX)
                TrimEndInTime = TrimEndScreentoCanvas(TrimEndOverlayMaxX);
            else
                TrimEndInTime = TrimEndScreentoCanvas((float)p.X);//位置转时间

            //时间跨度不超出限制
            if (TrimEndInTime < TimeSpan.Zero)
                TrimEndInTime = TimeSpan.Zero;
            else if (TrimEndInTime > App.Model.Current.OriginalDuration - App.Model.Current.TrimTimeFromStart)
                TrimEndInTime = App.Model.Current.OriginalDuration - App.Model.Current.TrimTimeFromStart;
            
            TrimEndInX = TrimEndCanvastoScreen(TrimEndInTime);//时间转位置
        }
        private void TrimEnd_Complete(Point p)
        {
            TrimEnd_Delta(p);

            App.Model.Current.TrimTimeFromEnd = TrimEndInTime;//裁切
        }



        private TimeSpan TrimEndScreentoCanvas(float X)
        {
            return App.Model.Current.TrimTimeFromEnd - App.Model.ScreenToCanvas(X) + App.Model.Current.EndTimeInComposition;
        }
        private float TrimEndCanvastoScreen(TimeSpan time)
        {
            return App.Model.CanvasToScreen(-time + (App.Model.Current.EndTimeInComposition + App.Model.Current.TrimTimeFromEnd));
        }


        #endregion


        #region OverlayMove：移动


        float OverlayMoveStartX;//移动起始点
        TimeSpan OverlayMoveStartTime;

        private void OverlayMove_Start(Point p)
        {
            OverlayMoveStartX = (float)p.X;
            OverlayMoveStartTime = App.Model.OverlayCurrent.Delay;
        }
        private void OverlayMove_Delta(Point p)
        {
             if (App.Model.OverlayCurrent != null)
            {
                TimeSpan Delay = OverlayMoveStartTime + App.Model.ImageToCanvas((float)p.X - OverlayMoveStartX);

                //不超过主媒体剪辑的左右限制
                if (Delay < TimeSpan.Zero)
                    App.Model.OverlayCurrent.Delay = TimeSpan.Zero;
                else if (Delay > App.Model.MediaComposition.Duration - App.Model.OverlayCurrent.Clip.TrimmedDuration)
                    App.Model.OverlayCurrent.Delay = App.Model.MediaComposition.Duration - App.Model.OverlayCurrent.Clip.TrimmedDuration;
                else
                    App.Model.OverlayCurrent.Delay = Delay;
            }
        }
        private void OverlayMove_Complete(Point p)
        {
            OverlayMove_Delta(p);
        }


        #endregion

        #region OverlayTrimStart：覆盖裁切


        float OverlayTrimStartInX;//裁切开始位置，用于画布渲染
        TimeSpan OverlayTrimStartInTime;//裁切开始时间，用于画布渲染

        private void OverlayTrimStart_Start(Point p)
        {
            OverlayTrimStartInX = App.Model.CanvasToScreen(App.Model.OverlayCurrent.Clip.StartTimeInComposition);   //先从当前的媒体剪辑，获取裁切开始位置
        }
        private void OverlayTrimStart_Delta(Point p)
        {
            OverlayTrimStartInTime = OverlayTrimStartScreentoCanvas((float)p.X);//位置转时间

            //时间跨度不超出限制
            if (OverlayTrimStartInTime < TimeSpan.Zero)
                OverlayTrimStartInTime = TimeSpan.Zero;
            else if (OverlayTrimStartInTime > App.Model.OverlayCurrent.Clip.OriginalDuration - App.Model.OverlayCurrent.Clip.TrimTimeFromEnd)
                OverlayTrimStartInTime = App.Model.OverlayCurrent.Clip.OriginalDuration - App.Model.OverlayCurrent.Clip.TrimTimeFromEnd;

            OverlayTrimStartInX = OverlayTrimStartCanvastoScreen(OverlayTrimStartInTime);//时间转位置
        }
        private void OverlayTrimStart_Complete(Point p)
        {
            OverlayTrimStart_Delta(p);

              

            //左剪裁后，要进行相应的位移
            TimeSpan Delay = App.Model.OverlayCurrent.Delay - (App.Model.OverlayCurrent.Clip.TrimTimeFromStart - OverlayTrimStartInTime);

            //OverlayStart：时间跨度不超出限制
            if (Delay < TimeSpan.Zero)
                Delay = TimeSpan.Zero;
            else if (Delay > App.Model.OverlayCurrent.Clip.OriginalDuration)
                Delay = App.Model.OverlayCurrent.Clip.OriginalDuration;

            App.Model.OverlayCurrent.Delay = Delay;


 
            App.Model.OverlayCurrent.Clip.TrimTimeFromStart = OverlayTrimStartInTime;//裁切
        }




        private TimeSpan OverlayTrimStartScreentoCanvas(float X)
        {
            return App.Model.OverlayCurrent.Clip.TrimTimeFromStart + (App.Model.ScreenToCanvas(X) - App.Model.OverlayCurrent.Delay);
        }
        private float OverlayTrimStartCanvastoScreen(TimeSpan time)
        {
            return App.Model.CanvasToScreen(time - (App.Model.OverlayCurrent.Clip.TrimTimeFromStart - App.Model.OverlayCurrent.Delay));
        }


        #endregion

        #region OverlayTrimEnd：覆盖裁切


        float OverlayTrimEndInX;//裁切结束位置，用于画布渲染
        TimeSpan OverlayTrimEndInTime;//裁切结束时间，用于画布渲染


        private void OverlayTrimEnd_Start(Point p)
        {
            OverlayTrimEndInX = App.Model.CanvasToScreen(App.Model.OverlayCurrent.Clip.EndTimeInComposition);   //先从当前的媒体剪辑，获取裁切结束位置
        }
        private void OverlayTrimEnd_Delta(Point p)
        {
            OverlayTrimEndInTime = OverlayTrimEndScreentoCanvas((float)p.X);//位置转时间

            //时间跨度不超出限制
            if (OverlayTrimEndInTime < TimeSpan.Zero)
                OverlayTrimEndInTime = TimeSpan.Zero;
            else if (OverlayTrimEndInTime > App.Model.OverlayCurrent.Clip.OriginalDuration - App.Model.OverlayCurrent.Clip.TrimTimeFromStart)
                OverlayTrimEndInTime = App.Model.OverlayCurrent.Clip.OriginalDuration - App.Model.OverlayCurrent.Clip.TrimTimeFromStart;

            OverlayTrimEndInX = OverlayTrimEndCanvastoScreen(OverlayTrimEndInTime);//时间转位置
        }
        private void OverlayTrimEnd_Complete(Point p)
        {
            OverlayTrimEnd_Delta(p);

            App.Model.OverlayCurrent.Clip.TrimTimeFromEnd = OverlayTrimEndInTime;//裁切
        }




        private TimeSpan OverlayTrimEndScreentoCanvas(float X)
        {
            return App.Model.OverlayCurrent.Clip.TrimTimeFromEnd - App.Model.ScreenToCanvas(X) + App.Model.OverlayCurrent.Clip.EndTimeInComposition;
        }
        private float OverlayTrimEndCanvastoScreen(TimeSpan time)
        {
            return App.Model.CanvasToScreen(-time + (App.Model.OverlayCurrent.Clip.EndTimeInComposition + App.Model.OverlayCurrent.Clip.TrimTimeFromEnd));
        }


        #endregion



        #region AudioMove：移动


        float AudioMoveStartX;//移动起始点
        TimeSpan AudioMoveStartTime;

        private void AudioMove_Start(Point p)
        {
            AudioMoveStartX = (float)p.X;
            AudioMoveStartTime = App.Model.AudioCurrent.Delay;
        }
        private void AudioMove_Delta(Point p)
        {
            if (App.Model.AudioCurrent != null)
            {
                TimeSpan Delay = AudioMoveStartTime + App.Model.ImageToCanvas((float)p.X - AudioMoveStartX);

                //不超过主媒体剪辑的左右限制
                if (Delay < TimeSpan.Zero)
                    App.Model.AudioCurrent.Delay = TimeSpan.Zero;
                else if (Delay > App.Model.MediaComposition.Duration - App.Model.AudioCurrent.TrimmedDuration)
                    App.Model.AudioCurrent.Delay = App.Model.MediaComposition.Duration - App.Model.AudioCurrent.TrimmedDuration;
                 else
                    App.Model.AudioCurrent.Delay = Delay;
            }
        }
        private void AudioMove_Complete(Point p)
        {
            AudioMove_Delta(p);
        }


        #endregion

        #region AudioTrimStart：覆盖裁切


        float AudioTrimStartInX;//裁切开始位置，用于画布渲染
        TimeSpan AudioTrimStartInTime;//裁切开始时间，用于画布渲染

        private void AudioTrimStart_Start(Point p)
        {
            AudioTrimStartInX = App.Model.CanvasToScreen(App.Model.AudioCurrent.Delay);   //先从当前的媒体剪辑，获取裁切开始位置
        }
        private void AudioTrimStart_Delta(Point p)
        {
            AudioTrimStartInTime = AudioTrimStartScreentoCanvas((float)p.X);//位置转时间

            //时间跨度不超出限制
            if (AudioTrimStartInTime < TimeSpan.Zero)
                AudioTrimStartInTime = TimeSpan.Zero;
            else if (AudioTrimStartInTime > App.Model.AudioCurrent.OriginalDuration - App.Model.AudioCurrent.TrimTimeFromEnd)
                AudioTrimStartInTime = App.Model.AudioCurrent.OriginalDuration - App.Model.AudioCurrent.TrimTimeFromEnd;

            AudioTrimStartInX = AudioTrimStartCanvastoScreen(AudioTrimStartInTime);//时间转位置
        }
        private void AudioTrimStart_Complete(Point p)
        {
            AudioTrimStart_Delta(p);



            //左剪裁后，要进行相应的位移
            TimeSpan Delay = App.Model.AudioCurrent.Delay - (App.Model.AudioCurrent.TrimTimeFromStart - AudioTrimStartInTime);

            //AudioStart：时间跨度不超出限制
            if (Delay < TimeSpan.Zero)
                Delay = TimeSpan.Zero;
            else if (Delay > App.Model.AudioCurrent.OriginalDuration)
                Delay = App.Model.AudioCurrent.OriginalDuration;

            App.Model.AudioCurrent.Delay = Delay;



            App.Model.AudioCurrent.TrimTimeFromStart = AudioTrimStartInTime;//裁切
        }




        private TimeSpan AudioTrimStartScreentoCanvas(float X)
        {
            return App.Model.AudioCurrent.TrimTimeFromStart + (App.Model.ScreenToCanvas(X) - App.Model.AudioCurrent.Delay);
        }
        private float AudioTrimStartCanvastoScreen(TimeSpan time)
        {
            return App.Model.CanvasToScreen(time - (App.Model.AudioCurrent.TrimTimeFromStart - App.Model.AudioCurrent.Delay));
        }


        #endregion

        #region AudioTrimEnd：覆盖裁切


        float AudioTrimEndInX;//裁切结束位置，用于画布渲染
        TimeSpan AudioTrimEndInTime;//裁切结束时间，用于画布渲染


        private void AudioTrimEnd_Start(Point p)
        {
            AudioTrimEndInX = App.Model.CanvasToScreen(App.Model.AudioCurrent.Delay+App.Model.AudioCurrent.TrimmedDuration);   //先从当前的媒体剪辑，获取裁切结束位置
        }
        private void AudioTrimEnd_Delta(Point p)
        {
            AudioTrimEndInTime = AudioTrimEndScreentoCanvas((float)p.X);//位置转时间

            //时间跨度不超出限制
            if (AudioTrimEndInTime < TimeSpan.Zero)
                AudioTrimEndInTime = TimeSpan.Zero;
            else if (AudioTrimEndInTime > App.Model.AudioCurrent.OriginalDuration - App.Model.AudioCurrent.TrimTimeFromStart)
                AudioTrimEndInTime = App.Model.AudioCurrent.OriginalDuration - App.Model.AudioCurrent.TrimTimeFromStart;

            AudioTrimEndInX = AudioTrimEndCanvastoScreen(AudioTrimEndInTime);//时间转位置
        }
        private void AudioTrimEnd_Complete(Point p)
        {
            AudioTrimEnd_Delta(p);

            App.Model.AudioCurrent.TrimTimeFromEnd = AudioTrimEndInTime;//裁切
        }




        private TimeSpan AudioTrimEndScreentoCanvas(float X)
        {
            return App.Model.AudioCurrent.TrimTimeFromEnd - App.Model.ScreenToCanvas(X) + App.Model.AudioCurrent.Delay+App.Model.AudioCurrent.TrimmedDuration;
        }
        private float AudioTrimEndCanvastoScreen(TimeSpan time)
        {
            return App.Model.CanvasToScreen(-time + (App.Model.AudioCurrent.Delay + App.Model.AudioCurrent.TrimmedDuration + App.Model.AudioCurrent.TrimTimeFromEnd));
        }


        #endregion


        

    }
}