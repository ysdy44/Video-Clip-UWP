 using Windows.Media.Editing;
using System.ComponentModel;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Media;

namespace 剪片.Model
{
    public class Media : INotifyPropertyChanged
    {



        //媒体名称
        private string name;
        public string Name
        {
            get { return name; }
            set
            {
                name = value;
                this.OnPropertyChanged("Name");
            }
        }


        //媒体缩略图
        private BitmapImage bitmap;
        public BitmapImage Bitmap
        {
            get { return bitmap; }
            set
            {
                bitmap = value;
                this.OnPropertyChanged("Bitmap");
            }
        }


        //媒体颜色
        private SolidColorBrush brush;
        public SolidColorBrush Brush
        {
            get { return brush; }
            set
            {
                brush = value;
                this.OnPropertyChanged("Brush");
            }
        }


        //媒体剪辑
        private MediaClip clip;
        public MediaClip Clip
        {
            get { return clip; }
            set
            {
                clip = value;
                this.OnPropertyChanged("Clip");
            }
        }
         


        public Media()  { }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}
