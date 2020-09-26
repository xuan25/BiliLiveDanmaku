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
    /// GuardBuy.xaml 的交互逻辑
    /// </summary>
    public partial class GuardBuy : UserControl, FaceLoader.ILoadFace
    {
        public uint UserId { get; private set; }

        public void SetFace(BitmapImage faceImage)
        {
            FaceImage.Source = faceImage;
            ((System.Windows.Media.Animation.Storyboard)Resources["ShowFaceImage"]).Begin();
        }

        private static SolidColorBrush CaptainBrush => new SolidColorBrush(Color.FromArgb(0xFF, 0x64, 0xD2, 0xF0));
        private static SolidColorBrush AdmiralBrush => new SolidColorBrush(Color.FromArgb(0xFF, 0xFA, 0x82, 0xBE));
        private static SolidColorBrush GovernorBrush => new SolidColorBrush(Color.FromArgb(0xFF, 0xDC, 0x8C, 0x32));


        public GuardBuy()
        {
            InitializeComponent();
            FaceImage.Source = new BitmapImage(new Uri("http://i2.hdslb.com/bfs/face/ad21ee2add7f10bddb3a584129473dc46c694459.jpg"));
            ((System.Windows.Media.Animation.Storyboard)Resources["ShowFaceImage"]).Begin();
        }

        public GuardBuy(BiliLiveJsonParser.GuardBuy guardBuy)
        {
            InitializeComponent();

            UserBox.Text = guardBuy.User.Name;
            TitleBox.Text = guardBuy.GiftName;
            switch (guardBuy.GuardLevel)
            {
                case 1:
                    InfoGrid.Background = GovernorBrush;
                    break;
                case 2:
                    InfoGrid.Background = AdmiralBrush;
                    break;
                case 3:
                    InfoGrid.Background = CaptainBrush;
                    break;
            }

            UserId = guardBuy.User.Id;

            FaceImage.Source = null;
            //BitmapImage bitmapImage = FaceLoader.LoadFormCache(UserId);
            //FaceImage.Source = bitmapImage;
            //if (!FaceLoader.LoadFormCache(this))
            //    //SetFace(bitmapImage);
            //    //if(bitmapImage == null)
            //    FaceLoader.Enqueue(this);
            FaceLoader.LoadFace(this);
        }

        
    }
}
