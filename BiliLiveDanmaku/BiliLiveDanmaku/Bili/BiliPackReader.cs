using JsonUtil;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;

namespace BiliLive
{
    class BiliPackReader
    {
        public enum PackTypes
        {
            Unknow = -1,
            Popularity = 3,
            Command = 5,
            Heartbeat = 8
        }

        public interface IPack
        {
            PackTypes PackType { get; }
        }

        public class PopularityPack : IPack
        {
            public PackTypes PackType => PackTypes.Popularity;
            public uint Popularity { get; private set; }

            public PopularityPack(byte[] payload)
            {
                Popularity = BitConverter.ToUInt32(payload.Take(4).Reverse().ToArray(), 0);
            }
        }

        public class CommandPack : IPack
        {
            public PackTypes PackType => PackTypes.Command;
            public Json.Value Value { get; private set; }

            public CommandPack(byte[] payload)
            {
                string jstr = Encoding.UTF8.GetString(payload, 0, payload.Length);
                Value = Json.Parser.Parse(jstr);
            }
        }

        public class HeartbeatPack : IPack
        {
            public PackTypes PackType => PackTypes.Heartbeat;

            public HeartbeatPack(byte[] payload)
            {

            }
        }

        private enum DataTypes
        {
            Unknow = -1,
            Plain = 0,
            Bin = 1,
            Gz = 2
        }

        public Stream BaseStream { get; private set; }
        public ClientWebSocket BaseWebSocket { get; private set; }

        public BiliPackReader(Stream stream)
        {
            BaseStream = stream;
        }

        public BiliPackReader(ClientWebSocket webSocket)
        {
            BaseWebSocket = webSocket;
            BaseStream = new MemoryStream();
        }

        public IPack[] ReadPacksAsync()
        {
            if (BaseWebSocket != null)
            {
                ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[4096]);
                WebSocketReceiveResult webSocketReceiveResult = BaseWebSocket.ReceiveAsync(buffer, CancellationToken.None).GetAwaiter().GetResult();
                BaseStream.Position = 0;
                BaseStream.Write(buffer.Array, 0, webSocketReceiveResult.Count);
                BaseStream.Position = 0;
            }

            // Pack length (4)
            byte[] packLengthBuffer = ReadTcpStream(BaseStream, 4);
            int packLength = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(packLengthBuffer, 0));
            if (packLength < 16)
            {
                BaseStream.Flush();
                // TODO : 包长度过短
                throw new Exception();
            }

            // Header length (2)
            byte[] headerLengthBuffer = ReadTcpStream(BaseStream, 2);
            int headerLength = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(headerLengthBuffer, 0));
            if (headerLength != 16)
            {
                BaseStream.Flush();
                // TODO : 头部长度异常
                throw new Exception();
            }

            // Data type (2)
            byte[] dataTypeBuffer = ReadTcpStream(BaseStream, 2);
            int dataTypeCode = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(dataTypeBuffer, 0));
            DataTypes dataType;
            if (Enum.IsDefined(typeof(DataTypes), dataTypeCode))
            {
                dataType = (DataTypes)Enum.ToObject(typeof(DataTypes), dataTypeCode);
            }
            else
            {
                dataType = DataTypes.Unknow;
            }


            // Read pack type (4)
            byte[] packTypeBuffer = ReadTcpStream(BaseStream, 4);
            int packTypeCode = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(packTypeBuffer, 0));
            PackTypes packType;
            if (Enum.IsDefined(typeof(PackTypes), packTypeCode))
            {
                packType = (PackTypes)Enum.ToObject(typeof(PackTypes), packTypeCode);
            }
            else
            {
                packType = PackTypes.Unknow;
            }

            // Read split (4)
            byte[] splitBuffer = ReadTcpStream(BaseStream, 4);

            // Read payload
            int payloadLength = packLength - headerLength;
            byte[] payloadBuffer = ReadTcpStream(BaseStream, payloadLength);

            // Return
            switch (dataType)
            {
                case DataTypes.Plain:
                    switch (packType)
                    {
                        case PackTypes.Command:
                            return new CommandPack[] { new CommandPack(payloadBuffer) };
                        default:
                            // TODO : 未知包类型
                            throw new Exception();
                    }
                case DataTypes.Bin:
                    switch (packType)
                    {
                        case PackTypes.Popularity:
                            return new PopularityPack[] { new PopularityPack(payloadBuffer) };
                        case PackTypes.Heartbeat:
                            return new HeartbeatPack[] { new HeartbeatPack(payloadBuffer) };
                        default:
                            // TODO : 未知包类型
                            throw new Exception();
                    }
                case DataTypes.Gz:
                    List<IPack> packs = new List<IPack>();
                    using (MemoryStream compressedStream = new MemoryStream(payloadBuffer))
                    {
                        using (GZipStream gZipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
                        {
                            using (MemoryStream decompressedStream = new MemoryStream())
                            {
                                gZipStream.CopyTo(decompressedStream);
                                decompressedStream.Position = 0;

                                while (decompressedStream.Position != decompressedStream.Length)
                                {
                                    IPack[] innerPackes = new BiliPackReader(decompressedStream).ReadPacksAsync();
                                    packs.AddRange(innerPackes);
                                }

                            }
                        }
                    }
                    return packs.ToArray();
                default:
                    // TODO : 未知数据类型
                    throw new Exception();
            }

        }

        private byte[] ReadTcpStream(Stream stream, int length)
        {
            int position = 0;
            byte[] buffer = new byte[length];
            while (position != length)
            {
                position += stream.Read(buffer, position, buffer.Length - position);
            }
            return buffer;
        }
    }
}
