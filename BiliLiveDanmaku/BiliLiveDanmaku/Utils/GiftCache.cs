using BiliLiveHelper.Bili;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BiliLiveDanmaku.Utils
{
    class GiftCache
    {
        const double CacheExpireTime = 5;

        private static List<GiftCache> GiftCaches { get; set; }

        private static Thread CacheManagingThread = null;
        private static bool IsCacheManagingRunning = false;

        public static event EventHandler<GiftCache> CacheExpired;

        static GiftCache()
        {
            GiftCaches = new List<GiftCache>();
        }

        private static void StartCacheManageThread()
        {
            IsCacheManagingRunning = true;
            if (CacheManagingThread == null)
            {
                CacheManagingThread = new Thread(() =>
                {
                    while (IsCacheManagingRunning)
                    {
                        CleanCache();
                        if (GiftCaches.Count == 0)
                        {
                            // Spin for 30 seconds, if the list is always empty, exit.
                            int count = 0;
                            while (GiftCaches.Count == 0)
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
            List<GiftCache> cleanedGifts = new List<GiftCache>();
            lock (GiftCaches)
            {
                while (GiftCaches.Count > 0)
                {
                    GiftCache gift = GiftCaches[0];
                    if (gift.ActiveExpiredTime < DateTime.UtcNow)
                    {
                        GiftCaches.RemoveAt(0);
                        cleanedGifts.Add(gift);
                    }
                    else
                    {
                        break;
                    }
                }
            }
            for (int i = 0; i < cleanedGifts.Count; i++)
            {
                GiftCache gift = cleanedGifts[i];
                gift.Expired?.Invoke(gift, gift);
                CacheExpired?.Invoke(gift, gift);
            }
        }

        public static GiftCache AppendCache(BiliLiveJsonParser.Gift gift)
        {
            GiftCache giftCache = new GiftCache()
            {
                UserId = gift.Sender.Id,
                Username = gift.Sender.Name,
                GiftId = gift.GiftId,
                GiftName = gift.GiftName,
                Number = gift.Number,
                CoinType = gift.CoinType,
                Action = gift.Action
            };
            GiftCaches.Add(giftCache);
            StartCacheManageThread();
            return giftCache;
        }

        public static bool AppendToExist(BiliLiveJsonParser.Gift gift)
        {
            CleanCache();
            lock (GiftCaches)
            {
                foreach (GiftCache cachedGift in GiftCaches)
                {
                    if (gift.Sender.Id == cachedGift.UserId && gift.GiftId == cachedGift.GiftId)
                    {
                        cachedGift.AppendNumber(gift.Number);
                        cachedGift.Updated?.Invoke(cachedGift, cachedGift);
                        // Move to the end of the list
                        GiftCaches.Remove(cachedGift);
                        GiftCaches.Add(cachedGift);
                        return true;
                    }
                }
            }
            return false;
        }



        public uint UserId;
        public string Username;
        public uint GiftId;
        public string GiftName;
        public uint Number;
        public string CoinType;
        public string Action;
        public DateTime ActiveExpiredTime;

        public event EventHandler<GiftCache> Updated;
        public event EventHandler<GiftCache> Expired;

        public GiftCache()
        {
            ActiveExpiredTime = DateTime.UtcNow.AddSeconds(CacheExpireTime);
        }

        public void AppendNumber(uint count)
        {
            Number += count;
            ActiveExpiredTime = DateTime.UtcNow.AddSeconds(CacheExpireTime);
        }
    }
}
