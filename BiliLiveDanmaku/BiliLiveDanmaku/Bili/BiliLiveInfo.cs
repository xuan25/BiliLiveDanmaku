using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;

namespace BiliLive
{
    class BiliLiveInfo
    {
        public delegate void InfoUpdateDel(Info info);
        public event InfoUpdateDel InfoUpdate;

        private uint RoomId;
        private Thread InfoListenerThread;
        private Info CurrentInfo;

        public BiliLiveInfo(uint roomId)
        {
            RoomId = roomId;
        }

        public class Info
        {
            public string Title;
            public string ParentAreaName;
            public string AreaName;
            public string Description;
            public string Tags;
            public uint Attention;
            public uint Online;
            public uint LiveStatus;
        }

        public Info GetInfo(int timeout)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://api.live.bilibili.com/room/v1/Room/get_info?room_id=" + RoomId);
                if (timeout > 0)
                {
                    request.Timeout = timeout;
                }
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                string ret = new StreamReader(response.GetResponseStream()).ReadToEnd();
                Match match = Regex.Match(ret, "(" +
                    ".*\"attention\":(?<Attention>[0-9]+)" +
                    "|.*\"online\":(?<Online>[0-9]+)" +
                    "|.*\"description\":\"(?<Description>.*?[^\\\\]?)\"" +
                    "|.*\"live_status\":(?<LiveStatus>[0-9]+)" +
                    "|.*\"parent_area_name\":\"(?<ParentAreaName>.*?[^\\\\]?)\"" +
                    "|.*\"title\":\"(?<Title>.*?[^\\\\]?)\"" +
                    "|.*\"tags\":\"(?<Tags>.*?[^\\\\]?)\"" +
                    "|.*\"area_name\":\"(?<AreaName>.*?[^\\\\]?)\"" +
                    ")+");
                if (match.Success)
                    return new Info()
                    {
                        Title = match.Groups["Title"].Value,
                        ParentAreaName = match.Groups["ParentAreaName"].Value,
                        AreaName = match.Groups["AreaName"].Value,
                        Description = match.Groups["Description"].Value,
                        Tags = match.Groups["Tags"].Value,
                        Attention = uint.Parse(match.Groups["Attention"].Value),
                        Online = uint.Parse(match.Groups["Online"].Value),
                        LiveStatus = uint.Parse(match.Groups["LiveStatus"].Value)
                    };
                return null;
            }
            catch (WebException)
            {
                return null;
            }
        }

        public void UpdateInfo(int timeout)
        {
            Info info = GetInfo(timeout);
            if (info != null && info.GetHashCode() != CurrentInfo.GetHashCode())
            {
                CurrentInfo = info;
                InfoUpdate?.Invoke(CurrentInfo);
            }
        }

        public bool StartInfoListener(int timeout, int interval)
        {
            if (InfoListenerThread != null)
                InfoListenerThread.Abort();
            if (CurrentInfo == null)
                CurrentInfo = GetInfo(timeout);
            if (CurrentInfo == null)
                return false;
            InfoListenerThread = new Thread(delegate ()
            {
                while (true)
                {
                    Thread.Sleep(interval);
                    UpdateInfo(timeout);
                }
            });
            InfoListenerThread.Start();
            return true;
        }

        public void StopInfoListener()
        {
            if (InfoUpdate != null)
            {
                Delegate[] delegates = InfoUpdate.GetInvocationList();
                foreach (Delegate d in delegates)
                    InfoUpdate -= (InfoUpdateDel)d;
            }
            if (InfoListenerThread != null)
                InfoListenerThread.Abort();
        }
    }
}
