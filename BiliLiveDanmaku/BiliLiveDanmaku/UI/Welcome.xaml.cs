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
using static BiliLiveDanmaku.UI.FaceLoader;

namespace BiliLiveDanmaku.UI
{
    /// <summary>
    /// Welcome.xaml 的交互逻辑
    /// </summary>
    public partial class Welcome : UserControl, ILoadFace
    {
        public uint UserId { get; private set; }

        public void SetFace(BitmapImage faceImage)
        {
            FaceImage.Source = faceImage;
            ((System.Windows.Media.Animation.Storyboard)Resources["ShowFaceImage"]).Begin();
        }

        private static SolidColorBrush VipBrush => new SolidColorBrush(Color.FromArgb(0xFF, 0xFA, 0x82, 0xBE));
        private static SolidColorBrush SvipBrush => new SolidColorBrush(Color.FromArgb(0xFF, 0xDC, 0x8C, 0x32));

        private static SolidColorBrush CaptainBrush => new SolidColorBrush(Color.FromArgb(0xFF, 0x64, 0xD2, 0xF0));
        private static SolidColorBrush AdmiralBrush => new SolidColorBrush(Color.FromArgb(0xFF, 0xFA, 0x82, 0xBE));
        private static SolidColorBrush GovernorBrush => new SolidColorBrush(Color.FromArgb(0xFF, 0xDC, 0x8C, 0x32));

        public Welcome()
        {
            InitializeComponent();
            FaceImage.Source = new BitmapImage(new Uri("http://i2.hdslb.com/bfs/face/ad21ee2add7f10bddb3a584129473dc46c694459.jpg"));
            ((System.Windows.Media.Animation.Storyboard)Resources["ShowFaceImage"]).Begin();
        }

        public Welcome(BiliLiveJsonParser.Welcome welcome)
        {
            InitializeComponent();

            UserBox.Text = welcome.User.Name;
            if (welcome.Svip)
            {
                TitleBox.Text = "年费老爷";
                UserBox.Foreground = SvipBrush;
                TitleBox.Foreground = SvipBrush;
            }
            else
            {
                TitleBox.Text = "老爷";
                UserBox.Foreground = VipBrush;
                TitleBox.Foreground = VipBrush;
            }

            UserId = welcome.User.Id;

            FaceImage.Source = null;
            //BitmapImage bitmapImage = FaceLoader.LoadFormCache(UserId);
            //FaceImage.Source = bitmapImage;
            //if (!FaceLoader.LoadFormCache(this))
            //    //SetFace(bitmapImage);
            //    //if(bitmapImage == null)
            //    FaceLoader.Enqueue(this);
            FaceLoader.LoadFace(this);
        }

        public Welcome(BiliLiveJsonParser.WelcomeGuard welcomeGuard)
        {
            InitializeComponent();

            UserBox.Text = welcomeGuard.User.Name;
            switch (welcomeGuard.GuardLevel)
            {
                case 1:
                    TitleBox.Text = "总督";
                    UserBox.Foreground = GovernorBrush;
                    TitleBox.Foreground = GovernorBrush;
                    break;
                case 2:
                    TitleBox.Text = "提督";
                    UserBox.Foreground = AdmiralBrush;
                    TitleBox.Foreground = AdmiralBrush;
                    break;
                case 3:
                    TitleBox.Text = "舰长";
                    UserBox.Foreground = CaptainBrush;
                    TitleBox.Foreground = CaptainBrush;
                    break;
            }

            UserId = welcomeGuard.User.Id;

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
