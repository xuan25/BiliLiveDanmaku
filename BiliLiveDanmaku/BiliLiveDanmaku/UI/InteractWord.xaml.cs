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
    /// InteractWord.xaml 的交互逻辑
    /// </summary>
    public partial class InteractWord : UserControl, ILoadFace
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

        public InteractWord(BiliLiveJsonParser.InteractWord interactWord)
        {
            InitializeComponent();

            UserBox.Text = interactWord.User.Name;

            switch (interactWord.MessageType)
            {
                case BiliLiveJsonParser.InteractWord.MessageTypes.Enter:
                    InteractWordBox.Text = "进入了直播间";
                    break;
                case BiliLiveJsonParser.InteractWord.MessageTypes.Follow:
                    InteractWordBox.Text = "关注了直播间";
                    break;
                case BiliLiveJsonParser.InteractWord.MessageTypes.Share:
                    InteractWordBox.Text = "分享了直播间";
                    break;
            }
            
            UserId = interactWord.User.Id;

            FaceImage.Source = null;
            FaceLoader.LoadFace(this);
        }
    }
}
