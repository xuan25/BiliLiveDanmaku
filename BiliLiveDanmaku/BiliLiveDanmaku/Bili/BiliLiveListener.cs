using JsonUtil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace BiliLive
{
    class BiliLiveListener
    {

        public enum Protocols { Tcp, Ws, Wss };
        public Protocols Protocol { get; set; }

        public delegate void ConnectionEventHandler();
        public event ConnectionEventHandler Connected;
        public event ConnectionEventHandler Disconnected;

        public delegate void ConnectionFailedHandler(string message);
        public event ConnectionFailedHandler ConnectionFailed;

        public delegate void ServerHeartbeatRecievedHandler();
        public event ServerHeartbeatRecievedHandler ServerHeartbeatRecieved;

        public delegate void PopularityRecievedHandler(uint popularity);
        public event PopularityRecievedHandler PopularityRecieved;

        public delegate void JsonsRecievedHandler(Json.Value[] jsons);
        public event JsonsRecievedHandler JsonsRecieved;

        public delegate void ItemsRecievedHandler(BiliLiveJsonParser.IItem[] items);
        public event ItemsRecievedHandler ItemsRecieved;

        private TcpClient DanmakuTcpClient { get; set; }
        private ClientWebSocket DanmakuWebSocket { get; set; }
        private uint RoomId { get; set; }

        private BiliPackReader PackReader { get; set; }
        private BiliPackWriter PackWriter { get; set; }

        private Thread HeartbeatSenderThread { get; set; }
        private bool IsHeartbeatSenderRunning { get; set; }

        private Thread EventListenerThread { get; set; }
        private bool IsEventListenerRunning { get; set; }

        /// <summary>
        /// Constructor 
        /// </summary>
        /// <param name="roomId"></param>
        public BiliLiveListener(uint roomId, Protocols protocol)
        {
            IsHeartbeatSenderRunning = false;
            IsEventListenerRunning = false;
            RoomId = roomId;
            Protocol = protocol;
        }

        #region Public methods

        public Task<bool> ConnectAsync() => new Task<bool>(Connect);

        public bool Connect()
        {
            PingReply pingReply = null;
            try
            {
                pingReply = new Ping().Send("live.bilibili.com");
            }
            catch (Exception)
            {

            }
            if (pingReply == null || pingReply.Status != IPStatus.Success)
            {
                ConnectionFailed?.Invoke("网络连接失败");
                return false;
            }

            DanmakuServer danmakuServer = GetDanmakuServer(RoomId);
            if (danmakuServer == null)
                return false;

            switch (Protocol)
            {
                case Protocols.Tcp:
                    DanmakuTcpClient = GetTcpConnection(danmakuServer);
                    Stream stream = DanmakuTcpClient.GetStream();

                    stream.ReadTimeout = 30 * 1000 + 1000;
                    stream.WriteTimeout = 30 * 1000 + 1000;

                    PackReader = new BiliPackReader(stream);
                    PackWriter = new BiliPackWriter(stream);
                    break;
                case Protocols.Ws:
                    DanmakuWebSocket = GetWsConnection(danmakuServer);
                    PackReader = new BiliPackReader(DanmakuWebSocket);
                    PackWriter = new BiliPackWriter(DanmakuWebSocket);
                    break;
                case Protocols.Wss:
                    DanmakuWebSocket = GetWssConnection(danmakuServer);
                    PackReader = new BiliPackReader(DanmakuWebSocket);
                    PackWriter = new BiliPackWriter(DanmakuWebSocket);
                    break;
            }

            if (!InitConnection(danmakuServer))
            {
                Disconnect();
                return false;
            }

            StartEventListener();
            StartHeartbeatSender();

            Connected?.Invoke();
            return true;
        }

        public Task DisconnectAsync() => new Task(Disconnect);

        public void Disconnect()
        {
            StopEventListener();
            StopHeartbeatSender();
            if (DanmakuTcpClient != null)
                DanmakuTcpClient.Close();
            if (DanmakuWebSocket != null)
            {
                DanmakuWebSocket.CloseAsync(WebSocketCloseStatus.EndpointUnavailable, string.Empty, CancellationToken.None);
                DanmakuWebSocket.Abort();
                DanmakuWebSocket.Dispose();
            }
            Disconnected?.Invoke();
        }

        #endregion

        #region Connect to a DanmakuServer

        private class DanmakuServer
        {
            public long RoomId;
            public string Server;
            public int Port;
            public int WsPort;
            public int WssPort;
            public string Token;
        }

        private TcpClient GetTcpConnection(DanmakuServer danmakuServer)
        {
            TcpClient tcpClient = new TcpClient();
            tcpClient.Connect(danmakuServer.Server, danmakuServer.Port);
            return tcpClient;
        }

        private ClientWebSocket GetWsConnection(DanmakuServer danmakuServer)
        {
            ClientWebSocket clientWebSocket = new ClientWebSocket();
            clientWebSocket.ConnectAsync(new Uri($"ws://{danmakuServer.Server}:{danmakuServer.WsPort}/sub"), CancellationToken.None).GetAwaiter().GetResult();
            return clientWebSocket;
        }

        private ClientWebSocket GetWssConnection(DanmakuServer danmakuServer)
        {
            ClientWebSocket clientWebSocket = new ClientWebSocket();
            clientWebSocket.ConnectAsync(new Uri($"wss://{danmakuServer.Server}:{danmakuServer.WssPort}/sub"), CancellationToken.None).GetAwaiter().GetResult();
            return clientWebSocket;
        }

        private bool InitConnection(DanmakuServer danmakuServer)
        {
            Json.Value initMsg = new Json.Value.Object
            {
                ["uid"] = 0,
                ["roomid"] = danmakuServer.RoomId,
                ["protover"] = 2,
                ["platform"] = "web",
                ["clientver"] = "1.12.0",
                ["type"] = 2,
                ["key"] = danmakuServer.Token
            };

            try
            {
                PackWriter.SendMessage((int)BiliPackWriter.MessageType.CONNECT, initMsg.ToString());
                return true;
            }
            catch (SocketException)
            {
                ConnectionFailed?.Invoke("连接请求发送失败");
                return false;
            }
            catch (InvalidOperationException)
            {
                ConnectionFailed?.Invoke("连接请求发送失败");
                return false;
            }
            catch (IOException)
            {
                ConnectionFailed?.Invoke("连接请求发送失败");
                return false;
            }
        }

        #endregion

        #region Room info

        private long GetRealRoomId(long roomId)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://api.live.bilibili.com/room/v1/Room/room_init?id=" + roomId);
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                using (StreamReader streamReader = new StreamReader(response.GetResponseStream()))
                {
                    string result = streamReader.ReadToEnd();
                    Match match = Regex.Match(result, "\"room_id\":(?<RoomId>[0-9]+)");
                    if (match.Success)
                        return uint.Parse(match.Groups["RoomId"].Value);
                    return 0;
                }

            }
            catch (WebException)
            {
                ConnectionFailed?.Invoke("未能找到直播间");
                return -1;
            }

        }

        private DanmakuServer GetDanmakuServer(long roomId)
        {
            roomId = GetRealRoomId(roomId);
            if (roomId < 0)
            {
                return null;
            }
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://api.live.bilibili.com/room/v1/Danmu/getConf?room_id=" + roomId);
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Json.Value json = Json.Parser.Parse(response.GetResponseStream());
                if (json["code"] != 0)
                {
                    Console.Error.WriteLine("Error occurs when resolving dm servers");
                    Console.Error.WriteLine(json.ToString());
                    return null;
                }

                DanmakuServer danmakuServer = new DanmakuServer
                {
                    RoomId = roomId,
                    Server = json["data"]["host_server_list"][0]["host"],
                    Port = json["data"]["host_server_list"][0]["port"],
                    WsPort = json["data"]["host_server_list"][0]["ws_port"],
                    WssPort = json["data"]["host_server_list"][0]["wss_port"],
                    Token = json["data"]["token"]
                };

                return danmakuServer;

            }
            catch (WebException)
            {
                ConnectionFailed?.Invoke("直播间信息获取失败");
                return null;
            }

        }

        #endregion

        #region Heartbeat Sender

        private void StopHeartbeatSender()
        {
            IsHeartbeatSenderRunning = false;
            if (HeartbeatSenderThread != null)
                HeartbeatSenderThread.Abort();
        }

        private void StartHeartbeatSender()
        {
            StopHeartbeatSender();
            HeartbeatSenderThread = new Thread(delegate ()
            {
                IsHeartbeatSenderRunning = true;
                while (IsHeartbeatSenderRunning)
                {
                    try
                    {
                        PackWriter.SendMessage((int)BiliPackWriter.MessageType.HEARTBEAT, "[object Object]");
                    }
                    catch (SocketException)
                    {
                        ConnectionFailed?.Invoke("心跳包发送失败");
                        Disconnect();
                    }
                    catch (InvalidOperationException)
                    {
                        ConnectionFailed?.Invoke("心跳包发送失败");
                        Disconnect();
                    }
                    catch (IOException)
                    {
                        ConnectionFailed?.Invoke("心跳包发送失败");
                        Disconnect();
                    }
                    Thread.Sleep(30 * 1000);
                }
            });
            HeartbeatSenderThread.Start();
        }

        #endregion

        #region Event listener

        private void StopEventListener()
        {
            IsEventListenerRunning = false;
            if (EventListenerThread != null)
                EventListenerThread.Abort();
        }

        private void StartEventListener()
        {
            EventListenerThread = new Thread(delegate ()
            {
                IsEventListenerRunning = true;
                while (IsEventListenerRunning)
                {
                    try
                    {
                        BiliPackReader.IPack[] packs = PackReader.ReadPacksAsync();

                        List<Json.Value> jsons = new List<Json.Value>();
                        List<BiliLiveJsonParser.IItem> items = new List<BiliLiveJsonParser.IItem>();

                        foreach (BiliPackReader.IPack pack in packs)
                        {
                            switch (pack.PackType)
                            {
                                case BiliPackReader.PackTypes.Popularity:
                                    PopularityRecieved?.Invoke(((BiliPackReader.PopularityPack)pack).Popularity);
                                    break;
                                case BiliPackReader.PackTypes.Command:
                                    Json.Value value = ((BiliPackReader.CommandPack)pack).Value;
                                    jsons.Add(value);
                                    BiliLiveJsonParser.IItem item = BiliLiveJsonParser.Parse(value);
                                    if (item != null)
                                        items.Add(item);
                                    break;
                                case BiliPackReader.PackTypes.Heartbeat:
                                    ServerHeartbeatRecieved?.Invoke();
                                    break;
                            }
                        }

                        if (jsons.Count > 0)
                        {
                            JsonsRecieved?.Invoke(jsons.ToArray());
                        }
                        if (items.Count > 0)
                        {
                            ItemsRecieved?.Invoke(items.ToArray());
                        }
                    }
                    catch (SocketException)
                    {
                        ConnectionFailed?.Invoke("数据读取失败");
                        Disconnect();
                    }
                    catch (IOException)
                    {
                        ConnectionFailed?.Invoke("数据读取失败");
                        Disconnect();
                    }
                }
            });
            EventListenerThread.Start();
        }

        #endregion
    }
}
