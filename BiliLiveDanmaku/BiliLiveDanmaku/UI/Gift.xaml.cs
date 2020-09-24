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
using static BiliLiveDanmaku.UI.FaceLoader;

namespace BiliLiveDanmaku.UI
{
    /// <summary>
    /// Gift.xaml 的交互逻辑
    /// </summary>
    public partial class Gift : UserControl, ILoadFace
    {
        // Activated Gift Cache sorted by expired time.
        private static List<Gift> ActivedGiftCache { get; set; }

        private static Thread CacheManagingThread = null;
        private static bool IsCacheManagingRunning = false;

        public static event EventHandler<Gift> GiftActiveExpired;

        static Gift()
        {
            ActivedGiftCache = new List<Gift>();
        }

        private static void StartCacheManageThread()
        {
            IsCacheManagingRunning = true;
            if(CacheManagingThread == null)
            {
                CacheManagingThread = new Thread(() =>
                {
                    while (IsCacheManagingRunning)
                    {
                        CleanCache();
                        if(ActivedGiftCache.Count == 0)
                        {
                            // Spin for 30 seconds, if the list is always empty, exit.
                            int count = 0;
                            while(ActivedGiftCache.Count == 0)
                            {
                                count++;
                                if (count > 30)
                                {
                                    IsCacheManagingRunning = false;
                                }
                                Thread.Sleep(1000);
                            }
                        }
                        else
                        {
                            Thread.Sleep(1000);
                        }
                    }
                    CacheManagingThread = null;
                })
                {
                    IsBackground = true,
                    Name = "Gift.CacheManagingThread"
                };
                CacheManagingThread.Start();
            }
        }

        private static void CleanCache()
        {
            lock (ActivedGiftCache)
            {
                while(ActivedGiftCache.Count > 0)
                {
                    Gift gift = ActivedGiftCache[0];
                    if (gift.ActiveExpiredTime < DateTime.UtcNow)
                    {
                        ActivedGiftCache.RemoveAt(0);
                        GiftActiveExpired?.Invoke(gift, gift);
                    }
                    else
                    {
                        break;
                    } 
                }
            }
        }

        private static void AppendCache(Gift gift)
        {
            ActivedGiftCache.Add(gift);
            StartCacheManageThread();
        }

        public static bool AppendGiftToExist(BiliLiveJsonParser.Gift gift)
        {
            CleanCache();
            lock (ActivedGiftCache)
            {
                foreach (Gift cachedGift in ActivedGiftCache)
                {
                    if (gift.Sender.Id == cachedGift.UserId && gift.GiftId == cachedGift.GiftId)
                    {
                        cachedGift.AppendNumber(gift.Number);
                        // Move to the end of the list
                        ActivedGiftCache.Remove(cachedGift);
                        ActivedGiftCache.Add(cachedGift);
                        return true;
                    }
                }
            }
            return false;
        }



        public void SetFace(BitmapImage faceImage)
        {
            FaceImage.Source = faceImage;
            ((System.Windows.Media.Animation.Storyboard)Resources["ShowFaceImage"]).Begin();
        }

        const double ActiceInterval = 5;

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

        public BiliLiveJsonParser.Gift Raw { get; private set; }

        public uint UserId { get; private set; }
        public uint GiftId { get; private set; }
        public DateTime ActiveExpiredTime { get; private set; }

        public Gift(BiliLiveJsonParser.Gift gift)
        {
            InitializeComponent();

            Raw = gift;

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

            ActiveExpiredTime = DateTime.UtcNow.AddSeconds(ActiceInterval);
            Gift.AppendCache(this);
        }

        public void AppendNumber(uint count)
        {
            CountNumber += count;
            ActiveExpiredTime = DateTime.UtcNow.AddSeconds(ActiceInterval);
        }
    }
}
