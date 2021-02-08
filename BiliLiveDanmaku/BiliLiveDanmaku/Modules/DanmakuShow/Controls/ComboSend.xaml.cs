using BiliLive;
using BiliLiveDanmaku.Common;
using System;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace DanmakuShow.Controls
{
    /// <summary>
    /// Gift.xaml 的交互逻辑
    /// </summary>
    public partial class ComboSend : UserControl, FaceLoader.ILoadFace
    {
        public void SetFace(BitmapImage faceImage)
        {
            FaceImage.Source = faceImage;
            ((System.Windows.Media.Animation.Storyboard)Resources["ShowFaceImage"]).Begin();
        }

        public ComboSend()
        {
            InitializeComponent();
            FaceImage.Source = new BitmapImage(new Uri("http://i2.hdslb.com/bfs/face/ad21ee2add7f10bddb3a584129473dc46c694459.jpg"));
            ((System.Windows.Media.Animation.Storyboard)Resources["ShowFaceImage"]).Begin();
        }

        public uint UserId { get; private set; }
        public uint GiftId { get; private set; }

        public ComboSend(BiliLiveJsonParser.ComboSend comboSend)
        {
            InitializeComponent();

            SenderBox.Text = comboSend.Sender.Name;
            ActionBox.Text = comboSend.Action;
            GiftBox.Text = comboSend.GiftName;
            NumBox.Text = comboSend.Number.ToString();

            UserId = comboSend.Sender.Id;
            GiftId = comboSend.GiftId;

            FaceImage.Source = null;
            //FaceImage.Source = FaceLoader.LoadFace(gift.Sender.Id, gift.FaceUri);
            FaceLoader.LoadFace(this);
        }
    }
}
