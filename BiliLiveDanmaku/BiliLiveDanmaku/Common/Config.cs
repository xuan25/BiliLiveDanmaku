using System;
using System.Collections.Generic;
using System.Windows;

namespace BiliLiveDanmaku.Common
{
    [Serializable]
    class Config
    {
        public Rect WindowRect { get; set; }
        public string RoomId { get; set; }
        public string OutputDevice { get; set; }
        public double Volume { get; set; }
        public Dictionary<FilterOptions, bool> FilterValueDict { get; set; }
    }
}
