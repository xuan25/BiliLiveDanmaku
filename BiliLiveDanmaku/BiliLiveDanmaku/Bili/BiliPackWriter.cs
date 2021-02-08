using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;

namespace BiliLive
{
    class BiliPackWriter
    {
        public Stream BaseStream { get; private set; }
        public ClientWebSocket BaseWebSocket { get; private set; }

        public BiliPackWriter(Stream stream)
        {
            BaseStream = stream;
        }

        public BiliPackWriter(ClientWebSocket webSocket)
        {
            BaseWebSocket = webSocket;
            BaseStream = new MemoryStream();
        }

        public enum MessageType { CONNECT = 7, HEARTBEAT = 2 };

        public void SendMessage(int messageType, string message)
        {
            byte[] messageArray = Encoding.UTF8.GetBytes(message);
            int dataLength = messageArray.Length + 16;

            MemoryStream buffer = new MemoryStream(dataLength);
            // Data length (4)
            buffer.Write(ToBE(BitConverter.GetBytes(dataLength)), 0, 4);
            // Header length and Data type (4)
            buffer.Write(new byte[] { 0x00, 0x10, 0x00, 0x01 }, 0, 4);
            // Message type (4)
            buffer.Write(ToBE(BitConverter.GetBytes(messageType)), 0, 4);
            // Split (4)
            buffer.Write(ToBE(BitConverter.GetBytes(1)), 0, 4);
            // Message
            buffer.Write(messageArray, 0, messageArray.Length);

            BaseStream.Write(buffer.GetBuffer(), 0, dataLength);
            BaseStream.Flush();

            if (BaseWebSocket != null)
            {
                byte[] b = new byte[dataLength];
                BaseStream.Position = 0;
                BaseStream.Read(b, 0, dataLength);
                BaseWebSocket.SendAsync(new ArraySegment<byte>(b), WebSocketMessageType.Binary, true, CancellationToken.None).GetAwaiter().GetResult();
                BaseStream.Position = 0;
            }
        }

        private static byte[] ToBE(byte[] b)
        {
            if (BitConverter.IsLittleEndian)
                Array.Reverse(b);
            return b;
        }
    }
}
