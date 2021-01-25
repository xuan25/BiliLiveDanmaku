using BiliLiveHelper.Bili;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BiliLiveDanmaku.Utils
{
    public class GiftCacheManager
    {
        public double CacheExpireDuration { get; private set; }

        private List<GiftCache> GiftCaches { get; set; }

        private Thread CacheManagingThread = null;
        private bool IsCacheManagingRunning = false;

        public event EventHandler<GiftCache> CacheExpired;

        public GiftCacheManager(double cacheExpireDuration)
        {
            CacheExpireDuration = cacheExpireDuration;
            GiftCaches = new List<GiftCache>();
        }

        private void StartCacheManageThread()
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
                                    break;
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

        private void CleanCache()
        {
            List<GiftCache> cleanedGifts = new List<GiftCache>();
            lock (GiftCaches)
            {
                DateTime now = DateTime.UtcNow;
                while (GiftCaches.Count > 0)
                {
                    GiftCache gift = GiftCaches[0];
                    if (gift.ExpiredTime < now)
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
                gift.OnExpire();
                CacheExpired?.Invoke(gift, gift);
            }
        }

        public GiftCache AppendCache(BiliLiveJsonParser.Gift gift)
        {
            GiftCache giftCache = new GiftCache(DateTime.UtcNow.AddSeconds(CacheExpireDuration))
            {
                UserId = gift.Sender.Id,
                Username = gift.Sender.Name,
                GiftId = gift.GiftId,
                GiftName = gift.GiftName,
                Number = gift.Number,
                CoinType = gift.CoinType,
                Action = gift.Action,
                FaceUri = gift.FaceUri
            };
            GiftCaches.Add(giftCache);
            StartCacheManageThread();
            return giftCache;
        }

        public bool AppendToExist(BiliLiveJsonParser.Gift gift)
        {
            CleanCache();
            lock (GiftCaches)
            {
                foreach (GiftCache cachedGift in GiftCaches)
                {
                    if (gift.Sender.Id == cachedGift.UserId && gift.GiftId == cachedGift.GiftId)
                    {
                        cachedGift.AppendNumber(gift.Number, DateTime.UtcNow.AddSeconds(CacheExpireDuration));
                        cachedGift.OnUpdate();
                        // Move to the end of the list
                        GiftCaches.Remove(cachedGift);
                        GiftCaches.Add(cachedGift);
                        return true;
                    }
                }
            }
            return false;
        }

        public class GiftCache
        {
            public uint UserId;
            public string Username;
            public uint GiftId;
            public string GiftName;
            public uint Number;
            public string CoinType;
            public string Action;
            public string FaceUri;
            public DateTime ExpiredTime;

            public event EventHandler<GiftCache> Updated;
            public event EventHandler<GiftCache> Expired;

            public GiftCache(DateTime expirTime)
            {
                ExpiredTime = expirTime;
            }

            public void AppendNumber(uint count, DateTime expireTime)
            {
                Number += count;
                ExpiredTime = expireTime;
            }

            public void OnUpdate()
            {
                Updated?.Invoke(this, this);
            }

            public void OnExpire()
            {
                Expired?.Invoke(this, this);
            }
        }
    }
}
