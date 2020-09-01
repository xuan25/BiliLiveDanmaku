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
    /// Gift.xaml 的交互逻辑
    /// </summary>
    public partial class ComboSend : UserControl, ILoadFace
    {
        public void SetFace(BitmapImage faceImage)
        {
            FaceImage.Source = faceImage;
        }

        public ComboSend()
        {
            InitializeComponent();
            FaceImage.Source = new BitmapImage(new Uri("http://i2.hdslb.com/bfs/face/ad21ee2add7f10bddb3a584129473dc46c694459.jpg"));
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
