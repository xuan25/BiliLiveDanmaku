using BiliLiveDanmaku.Utils;
using BiliLiveHelper.Bili;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

namespace BiliLiveDanmaku.UI
{
    /// <summary>
    /// SuperChat.xaml 的交互逻辑
    /// </summary>
    public partial class SuperChat : UserControl, FaceLoader.ILoadFace
    {
        public uint UserId { get; private set; }

        public void SetFace(BitmapImage faceImage)
        {
            FaceImage.Source = faceImage;
            ((System.Windows.Media.Animation.Storyboard)Resources["ShowFaceImage"]).Begin();
        }

        public SuperChat()
        {
            InitializeComponent();
            FaceImage.Source = new BitmapImage(new Uri("http://i2.hdslb.com/bfs/face/ad21ee2add7f10bddb3a584129473dc46c694459.jpg"));
            ((System.Windows.Media.Animation.Storyboard)Resources["ShowFaceImage"]).Begin();

            if (new Random((int)DateTime.Now.Ticks).NextDouble() < 0.5)
            {
                MessageBox.Text = Guid.NewGuid().ToString();
                MessageTransBorder.Visibility = Visibility.Collapsed;
            }
            else
            {
                MessageBox.Text = Guid.NewGuid().ToString();
                MessageTransBox.Text = Guid.NewGuid().ToString();
            }
            
        }

        public SuperChat(BiliLiveJsonParser.SuperChat superChat)
        {
            InitializeComponent();

            SenderBox.Text = superChat.User.Name;
            PriceBox.Text = superChat.Price.ToString();
            MessageBox.Text = superChat.Message;

            if (superChat.TransMark)
                MessageTransBox.Text = superChat.MessageTrans;
            else
                MessageTransBorder.Visibility = Visibility.Collapsed;

            InfoGrid.Background = new SolidColorBrush(superChat.PriceColor);
            MessageStackPanel.Background = new SolidColorBrush(superChat.BottomColor);

            UserId = superChat.User.Id;

            FaceImage.Source = null;
            //FaceImage.Source = FaceLoader.LoadFace(superChat.User.Id, superChat.Face);
            FaceLoader.LoadFaceWithKnownUri(this, superChat.Face);
        }

        private void MessageBox_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Clipboard.SetText(MessageBox.Text);
        }

        private void MessageTransBox_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Clipboard.SetText(MessageTransBox.Text);
        }
    }
}
