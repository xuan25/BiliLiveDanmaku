using BiliLiveDanmaku.Utils;
using BiliLiveHelper.Bili;
using System;
using System.Collections.Generic;
using System.Linq;
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

namespace BiliLiveDanmaku.UI
{
    /// <summary>
    /// Gift.xaml 的交互逻辑
    /// </summary>
    public partial class Gift : UserControl, FaceLoader.ILoadFace
    {
        public void SetFace(BitmapImage faceImage)
        {
            FaceImage.Source = faceImage;
            ((System.Windows.Media.Animation.Storyboard)Resources["ShowFaceImage"]).Begin();
        }

        public Gift()
        {
            InitializeComponent();
            FaceImage.Source = new BitmapImage(new Uri("http://i2.hdslb.com/bfs/face/ad21ee2add7f10bddb3a584129473dc46c694459.jpg"));
        }

        private static SolidColorBrush SilverBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0xCB, 0xDA, 0xF7));
        private static SolidColorBrush GoldBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0xDC, 0x8C, 0x32));

        private uint number;
        public uint Number
        {
            get
            {
                return number;
            }
            set
            {
                number = value;
                NumBox.Text = value.ToString();
            }
        }

        public uint UserId { get; private set; }

        public Gift(BiliLiveJsonParser.Gift gift)
        {
            InitializeComponent();

            SenderBox.Text = gift.Sender.Name;
            ActionBox.Text = gift.Action;
            GiftBox.Text = gift.GiftName;
            if(gift.CoinType == "gold")
            {
                // "gold"
                GiftBox.Foreground = GoldBrush;
            }
            else
            {
                // "silver"
                GiftBox.Foreground = SilverBrush;
            }
            Number = gift.Number;

            UserId = gift.Sender.Id;

            FaceImage.Source = null;
            //FaceImage.Source = FaceLoader.LoadFace(gift.Sender.Id, gift.FaceUri);
            FaceLoader.LoadFaceWithKnownUri(this, gift.FaceUri);

            GiftCache giftCache = GiftCache.AppendCache(gift);
            giftCache.Updated += GiftCache_GiftUpdated;
        }

        private void GiftCache_GiftUpdated(object sender, GiftCache e)
        {
            Dispatcher.Invoke(() =>
            {
                Number = e.Number;
            });
        }
    }
}
