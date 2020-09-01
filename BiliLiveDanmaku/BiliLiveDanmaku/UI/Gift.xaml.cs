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
    public partial class Gift : UserControl, ILoadFace
    {
        public void SetFace(BitmapImage faceImage)
        {
            FaceImage.Source = faceImage;
        }

        private static List<Gift> GiftAppendCache { get; set; }

        static Gift()
        {
            GiftAppendCache = new List<Gift>();
        }

        public static bool AppendGiftToExist(BiliLiveJsonParser.Gift gift)
        {
            CleanCache();
            lock (GiftAppendCache)
            {
                foreach(Gift cachedGift in GiftAppendCache)
                {
                    if(gift.Sender.Id == cachedGift.UserId && gift.GiftId == cachedGift.GiftId)
                    {
                        cachedGift.AppendNumber(gift.Number);
                        return true;
                    }
                }
            }
            return false;
        }

        public static void CleanCache()
        {
            lock (GiftAppendCache)
            {
                while(GiftAppendCache.Count > 0)
                {
                    if (GiftAppendCache[0].LastUpdateAllowedTime < DateTime.UtcNow)
                        GiftAppendCache.RemoveAt(0);
                    else
                        break;
                }
            }
        }

        public Gift()
        {
            InitializeComponent();
            FaceImage.Source = new BitmapImage(new Uri("http://i2.hdslb.com/bfs/face/ad21ee2add7f10bddb3a584129473dc46c694459.jpg"));
        }

        private static SolidColorBrush SilverBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0xCB, 0xDA, 0xF7));
        private static SolidColorBrush GoldBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0xDC, 0x8C, 0x32));

        private uint _CountNumber;
        public uint CountNumber
        {
            get
            {
                return _CountNumber;
            }
            set
            {
                _CountNumber = value;
                NumBox.Text = value.ToString();
            }
        }

        public uint UserId { get; private set; }
        public uint GiftId { get; private set; }
        public DateTime LastUpdateAllowedTime { get; private set; }

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
            CountNumber = gift.Number;

            UserId = gift.Sender.Id;
            GiftId = gift.GiftId;

            FaceImage.Source = null;
            //FaceImage.Source = FaceLoader.LoadFace(gift.Sender.Id, gift.FaceUri);
            FaceLoader.LoadFaceWithKnownUri(this, gift.FaceUri);

            LastUpdateAllowedTime = DateTime.UtcNow.AddSeconds(5);
            GiftAppendCache.Add(this);
        }

        public void AppendNumber(uint count)
        {
            CountNumber += count;
            LastUpdateAllowedTime = DateTime.UtcNow.AddSeconds(5);
        }
    }
}
