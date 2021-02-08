using BiliLive;
using System.Windows.Controls;

namespace DanmakuShow.Controls
{
    /// <summary>
    /// RoomBlock.xaml 的交互逻辑
    /// </summary>
    public partial class RoomBlock : UserControl
    {
        public uint UserId { get; private set; }

        public RoomBlock()
        {
            InitializeComponent();
        }

        public RoomBlock(BiliLiveJsonParser.RoomBlock roomBlock)
        {
            InitializeComponent();

            UserBox.Text = roomBlock.User.Name;

            UserId = roomBlock.User.Id;
        }
    }
}
