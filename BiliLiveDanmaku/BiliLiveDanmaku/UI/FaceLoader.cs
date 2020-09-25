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
            public event EventHandler<BitmapImage> DownloadCompleted;

            public BitmapImage FaceImage;
            public bool IsLoading;
            public DateTime Expire;

            public FaceCache(Uri uri, ILoadFace owner)
            {
                uri = new Uri(uri.AbsoluteUri + "@24w_24h_1c_100q.jpg");
                IsLoading = true;
                Expire = DateTime.UtcNow.AddMinutes(30);

                Task.Factory.StartNew(() =>
                {
                    MemoryStream memoryStream = null;
                    int retryCounter = 0;
                    while (retryCounter < 3)
                    {
                        try
                        {
                            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(uri);
                            httpWebRequest.ServicePoint.ConnectionLimit = 16;
                            using (HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse())
                            {
                                using (Stream stream = httpWebResponse.GetResponseStream())
                                {
                                    memoryStream = new MemoryStream();
                                    stream.CopyTo(memoryStream);
                                    memoryStream.Position = 0;
                                }
                            }
                            break;
                        }
                        catch (WebException ex)
                        {
                            Console.WriteLine(ex);
                            if (memoryStream != null)
                            {
                                memoryStream.Dispose();
                                memoryStream = null;
                            }
                            if (ex.Response != null && ((HttpWebResponse)ex.Response).StatusCode == HttpStatusCode.NotFound)
                            {
                                break;
                            }
                            retryCounter++;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                            if (memoryStream != null)
                            {
                                memoryStream.Dispose();
                                memoryStream = null;
                            }
                            break;
                        }
                    }

                    if(memoryStream == null)
                    {
                        return;
                    }


                    owner.Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            BitmapImage bitmapImage = new BitmapImage();

                            bitmapImage.BeginInit();
                            bitmapImage.StreamSource = memoryStream;
                            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                            bitmapImage.EndInit();

                            bitmapImage.Freeze();

                            FaceImage = bitmapImage;
                            memoryStream.Close();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                            memoryStream.Close();
                        }
                        
                    });

                    IsLoading = false;
                    DownloadCompleted?.Invoke(this, FaceImage);

                    ApplyTo(owner);
                });
            }

            public void ApplyTo(ILoadFace loadFace)
            {
                if (IsLoading)
                {
                    DownloadCompleted += (object sender, BitmapImage e) =>
                    {
                        loadFace.Dispatcher.Invoke(() =>
                        {
                            loadFace.SetFace(e);
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
                faceCache.Expire = DateTime.UtcNow.AddMinutes(30);
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

        // https://github.com/3Shain/BiliChat/issues/6
        const int requestInterval = 200;
        const int blockMinutes = 15;
        static DateTime bolckUntil = new DateTime();

        private static void LoadFaceAndWait(ILoadFace loadFace)
        {
            if (FaceCacheDict.ContainsKey(loadFace.UserId))
            {
                FaceCache faceCache = FaceCacheDict[loadFace.UserId];
                faceCache.Expire = DateTime.UtcNow.AddMinutes(30);
                faceCache.ApplyTo(loadFace);
            }
            else
            {
                if (DateTime.Now < bolckUntil)
                    return;

                DateTime startTime = DateTime.UtcNow;
                Uri uri;
                while (true)
                {
                    try
                    {
                        uri = LoadFaceUriFromApi(loadFace.UserId);
                        break;
                    }
                    catch (WebException ex)
                    {
                        if(ex.Status == WebExceptionStatus.Timeout)
                        {
                            Console.WriteLine(ex);
                            Thread.Sleep(1000);
                        }
                        else if (ex.Response != null && ((HttpWebResponse)ex.Response).StatusCode == HttpStatusCode.PreconditionFailed)
                        {
                            bolckUntil = DateTime.Now.AddMinutes(blockMinutes);
                            Console.WriteLine($"Face API has been blocked, expected until {bolckUntil}");
                            return;
                        }
                        else
                        {
                            Console.WriteLine(ex);
                            return;
                        }
                    }
                }
                
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
                int waitTime = requestInterval - (int)timeSpan.TotalMilliseconds;
                if (waitTime < 0)
                    waitTime = 0;
                Thread.Sleep(waitTime);
                //Thread.Sleep(requestInterval);
            }
        }

        private static Uri LoadFaceUriFromApi(uint uid)
        {
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(string.Format("https://api.bilibili.com/x/space/acc/info?mid={0}&jsonp=jsonp", uid));
            HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
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
