using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

//https://go.microsoft.com/fwlink/?LinkId=234236 上介绍了“用户控件”项模板

namespace 剪片.Picker
{
    public sealed partial class TipPicker : UserControl
    {
        #region DependencyProperty：依赖属性

        public Visibility TipVisibility
        {
            set { SetValue(TipVisibilityProperty, value); }
        }
        public static readonly DependencyProperty TipVisibilityProperty =
            DependencyProperty.Register("TipVisibility", typeof(Visibility), typeof(TipPicker), new PropertyMetadata(0, new PropertyChangedCallback(TipVisibilityOnChang)));
        private static void TipVisibilityOnChang(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            TipPicker Con = (TipPicker)sender;

            if ((Visibility)e.NewValue == Visibility.Visible)
                Con.TipShow.Begin();
            else if ((Visibility)e.NewValue == Visibility.Collapsed)
                Con.TipFade.Begin();
        }

        #endregion 

        public TipPicker()
        {
            this.InitializeComponent();
            this.DataContext = App.Model;
        }
    }
}
