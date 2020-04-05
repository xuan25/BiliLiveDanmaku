using JsonUtil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace BiliLiveDanmaku.UI
{
    class FaceLoader
    {
        public delegate void CachedCountChangedHandler(int count);
        public static event CachedCountChangedHandler CachedCountChanged;

        public delegate void QueueCountChangedHandler(int count);
        public static event QueueCountChangedHandler QueueCountChanged;

        public class FaceCache
        {
            public event EventHandler DownloadCompleted;

            public BitmapImage FaceImage;
            public bool IsDownloading;
            public DateTime Expire;

            public FaceCache(Uri uri, ILoadFace owner)
            {
                IsDownloading = true;
                Expire = DateTime.UtcNow.AddSeconds(300);

                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(uri);
                        HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                        Stream stream = httpWebResponse.GetResponseStream();

                        owner.Dispatcher.Invoke(() =>
                        {
                            BitmapImage bitmapImage = new BitmapImage();
                            FaceImage = bitmapImage;

                            bitmapImage.DownloadCompleted += FaceImage_DownloadCompleted;
                            bitmapImage.BeginInit();
                            bitmapImage.StreamSource = stream;
                            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                            bitmapImage.EndInit();

                            owner.SetFace(bitmapImage);
                        });
                    }
                    catch (WebException)
                    {

                    }
                    
                });

                
            }

            public void ApplyTo(ILoadFace loadFace)
            {
                if (IsDownloading)
                {
                    DownloadCompleted += (object sender, EventArgs e) =>
                    {
                        loadFace.Dispatcher.Invoke(() =>
                        {
                            loadFace.SetFace(FaceImage);
                        });
                    };
                }
                else
                {
                    loadFace.Dispatcher.Invoke(() =>
                    {
                        loadFace.SetFace(FaceImage);
                    });
                } 
            }

            private void FaceImage_DownloadCompleted(object sender, EventArgs e)
            {
                FaceImage.Freeze();
                FaceImage.StreamSource.Close();

                IsDownloading = false;
                DownloadCompleted?.Invoke(sender, e);
            }
        }

        public static Dictionary<uint, FaceCache> FaceCacheDict;

        static FaceLoader()
        {
            FaceCacheDict = new Dictionary<uint, FaceCache>();
        }

        public interface ILoadFace
        {
            /// <summary>
            /// Should be a DispatcherObject or implements a dispatcher
            /// </summary>
            Dispatcher Dispatcher { get; }

            /// <summary>
            /// User's uid
            /// </summary>
            uint UserId { get; }

            /// <summary>
            /// Callback method to update the face image
            /// </summary>
            /// <param name="faceImage"></param>
            void SetFace(BitmapImage faceImage);
        }

        public static void LoadFaceWithKnownUri(ILoadFace loadFace, string backupUri)
        {
            if (LoadFormCache(loadFace))
                return;

            Uri uri = new Uri(backupUri);
            
            
            Task.Factory.StartNew(() =>
            {
                FaceCache newFaceCache = new FaceCache(uri, loadFace);
                //newFaceCache.ApplyTo(loadFace);
                lock (FaceCacheDict)
                {
                    if (!FaceCacheDict.ContainsKey(loadFace.UserId))
                        FaceCacheDict.Add(loadFace.UserId, newFaceCache);
                }
                CleanCache();
                CachedCountChanged?.Invoke(FaceCacheDict.Count);
            });
        }

        public static void LoadFace(ILoadFace loadFace)
        {
            if (!LoadFormCache(loadFace))
                Enqueue(loadFace);
        }

        public static bool LoadFormCache(ILoadFace loadFace)
        {
            if (FaceCacheDict.ContainsKey(loadFace.UserId))
            {
                FaceCache faceCache = FaceCacheDict[loadFace.UserId];
                faceCache.Expire = DateTime.UtcNow.AddSeconds(300);
                faceCache.ApplyTo(loadFace);

                return true;
            }
            return false;
        }

        public static DateTime NextCleanTime = DateTime.UtcNow;

        private static void CleanCache()
        {
            if (NextCleanTime > DateTime.UtcNow)
                return;

            List<uint> removeMids = new List<uint>();

            lock (FaceCacheDict)
            {
                foreach (uint mid in FaceCacheDict.Keys)
                {
                    FaceCache faceCache = FaceCacheDict[mid];
                    if (faceCache.Expire < DateTime.UtcNow)
                        removeMids.Add(mid);
                }
                foreach (uint mid in removeMids)
                {
                    FaceCacheDict.Remove(mid);
                }
            }

            NextCleanTime = DateTime.UtcNow.AddSeconds(5);
            Console.WriteLine("Cache cleaned {0}/{1}", removeMids.Count, FaceCacheDict.Count + removeMids.Count);
        }


        static Queue<Action> FaceUriRequestQueue = new Queue<Action>();
        static Thread FaceUriRequestThread;
        private static void AppendFaceUriRequest(Action action)
        {
            if (FaceUriRequestThread == null || FaceUriRequestThread.ThreadState == ThreadState.Stopped)
            {
                FaceUriRequestThread = new Thread(new ThreadStart(() =>
                {
                    action.Invoke();
                    while (true)
                    {
                        int i = 0;
                        while (FaceUriRequestQueue.Count == 0)
                        {
                            Thread.Sleep(10);
                            i++;
                            if (i == 30*1000/10)
                                break;
                        }
                        if (FaceUriRequestQueue.Count == 0)
                            break;
                        Action nextAction = FaceUriRequestQueue.Dequeue();
                        nextAction.Invoke();
                        QueueCountChanged?.Invoke(FaceUriRequestQueue.Count);
                    }
                }))
                {
                    IsBackground = true,
                    Name = "FaceLoader.FaceUriRequestThread"
                };
                FaceUriRequestThread.Start();
            }
            else
            {
                FaceUriRequestQueue.Enqueue(action);
                QueueCountChanged?.Invoke(FaceUriRequestQueue.Count);
            }
        }

        public static void Enqueue(ILoadFace loadFace)
        {
            Action action = new Action(() =>
            {
                LoadFaceAndWait(loadFace);
                CleanCache();
                CachedCountChanged?.Invoke(FaceCacheDict.Count);
            });
            AppendFaceUriRequest(action);
        }

        private static void LoadFaceAndWait(ILoadFace loadFace)
        {
            if (FaceCacheDict.ContainsKey(loadFace.UserId))
            {
                FaceCache faceCache = FaceCacheDict[loadFace.UserId];
                faceCache.Expire = DateTime.UtcNow.AddSeconds(300);
                faceCache.ApplyTo(loadFace);
            }
            else
            {
                DateTime startTime = DateTime.UtcNow;
                Uri uri = LoadFaceUriFromApi(loadFace.UserId);
                if (uri != null)
                {
                    FaceCache faceCache = new FaceCache(uri, loadFace);
                    //faceCache.ApplyTo(loadFace);

                    lock (FaceCacheDict)
                    {
                        if (!FaceCacheDict.ContainsKey(loadFace.UserId))
                            FaceCacheDict.Add(loadFace.UserId, faceCache);
                    }
                }
                DateTime endTime = DateTime.UtcNow;
                TimeSpan timeSpan = endTime - startTime;
                int waitTime = 200 - (int)timeSpan.TotalMilliseconds;
                if (waitTime < 0)
                    waitTime = 0;
                Thread.Sleep(waitTime);
            }
        }

        private static Uri LoadFaceUriFromApi(uint uid)
        {
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(string.Format("https://api.bilibili.com/x/space/acc/info?mid={0}&jsonp=jsonp", uid));
            HttpWebResponse httpWebResponse;
            try
            {
                httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine(ex);
                return null;
            }
            string responseText;
            using (StreamReader streamReader = new StreamReader(httpWebResponse.GetResponseStream()))
            {
                responseText = streamReader.ReadToEnd();
            }
            Json.Value value = Json.Parser.Parse(responseText);
            if (value["code"] != 0)
                return null;
            Uri uri = new Uri(value["data"]["face"]);
            return uri;
        }
    }
}
