using BiliLive;
using BiliLiveDanmaku.Common;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DanmakuShow.Controls
{
    /// <summary>
    /// InteractWord.xaml 的交互逻辑
    /// </summary>
    public partial class InteractWord : UserControl, FaceLoader.ILoadFace
    {
        #region ILoadFace

        public uint UserId { get; private set; }

        public void SetFace(BitmapImage faceImage)
        {
            FaceImage.Source = faceImage;
            ((System.Windows.Media.Animation.Storyboard)Resources["ShowFaceImage"]).Begin();
        }

        #endregion

        public InteractWord()
        {
            InitializeComponent();
        }

        private static SolidColorBrush SilverBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0xAD, 0xBC, 0xD9));
        private static SolidColorBrush GoldBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0xDC, 0x8C, 0x32));

        public InteractWord(BiliLiveJsonParser.InteractWord interactWord)
        {
            InitializeComponent();

            UserBox.Text = interactWord.User.Name;

            switch (interactWord.MessageType)
            {
                case BiliLiveJsonParser.InteractWord.MessageTypes.Entry:
                    bool isGuard = false;
                    foreach (BiliLiveJsonParser.InteractWord.Identities identity in interactWord.Identity)
                    {
                        if ((int)identity >= (int)BiliLiveJsonParser.InteractWord.Identities.GuardJian)
                        {
                            isGuard = true;
                            break;
                        }
                    }
                    if (isGuard)
                    {
                        InteractWordBox.Text = "光临直播间";
                    }
                    else
                    {
                        InteractWordBox.Text = "进入直播间";
                    }
                    InteractWordBox.Foreground = SilverBrush;
                    break;
                case BiliLiveJsonParser.InteractWord.MessageTypes.Attention:
                    InteractWordBox.Text = "关注了直播间";
                    InteractWordBox.Foreground = GoldBrush;
                    break;
                case BiliLiveJsonParser.InteractWord.MessageTypes.Share:
                    InteractWordBox.Text = "分享了直播间";
                    InteractWordBox.Foreground = GoldBrush;
                    break;
                case BiliLiveJsonParser.InteractWord.MessageTypes.SpecialAttention:
                    InteractWordBox.Text = "特别关注了直播间";
                    InteractWordBox.Foreground = GoldBrush;
                    break;
                case BiliLiveJsonParser.InteractWord.MessageTypes.MutualAttention:
                    InteractWordBox.Text = "互粉了直播间";
                    InteractWordBox.Foreground = GoldBrush;
                    break;
            }

            UserId = interactWord.User.Id;

            FaceImage.Source = null;
            FaceLoader.LoadFace(this);
        }
    }
}
