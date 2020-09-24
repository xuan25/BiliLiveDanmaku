using BiliLiveHelper.Bili;
using JsonUtil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static BiliLiveDanmaku.UI.FaceLoader;

namespace BiliLiveDanmaku.UI
{
    /// <summary>
    /// Danmaku.xaml 的交互逻辑
    /// </summary>
    public partial class Danmaku : UserControl, ILoadFace
    {
        #region ILoadFace

        public uint UserId { get; private set; }

        public void SetFace(BitmapImage faceImage)
        {
            FaceImage.Source = faceImage;
            ((System.Windows.Media.Animation.Storyboard)Resources["ShowFaceImage"]).Begin();
        }

        #endregion

        public Danmaku()
        {
            InitializeComponent();
            MessageBox.Text = Guid.NewGuid().ToString();

            FaceImage.Source = new BitmapImage(new Uri("http://i2.hdslb.com/bfs/face/ad21ee2add7f10bddb3a584129473dc46c694459.jpg"));
            ((System.Windows.Media.Animation.Storyboard)Resources["ShowFaceImage"]).Begin();
        }

        public Danmaku(BiliLiveJsonParser.Danmaku danmaku)
        {
            InitializeComponent();

            SenderBox.Text = danmaku.Sender.Name;
            MessageBox.Text = danmaku.Message;

            UserId = danmaku.Sender.Id;

            FaceImage.Source = null;
            //BitmapImage bitmapImage = FaceLoader.LoadFormCache(UserId);
            //FaceImage.Source = bitmapImage;
            //if (!FaceLoader.LoadFormCache(this))
            //    //SetFace(bitmapImage);
            //    //if(bitmapImage == null)
            //    FaceLoader.Enqueue(this);
            FaceLoader.LoadFace(this);
        }

        private void MessageBox_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Clipboard.SetText(MessageBox.Text);
        }
    }
}
