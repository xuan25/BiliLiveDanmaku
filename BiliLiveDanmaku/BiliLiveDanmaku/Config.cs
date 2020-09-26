using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace BiliLiveDanmaku
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
