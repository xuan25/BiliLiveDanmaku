using BiliLiveHelper.Bili;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BiliLiveDanmaku.UI
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
