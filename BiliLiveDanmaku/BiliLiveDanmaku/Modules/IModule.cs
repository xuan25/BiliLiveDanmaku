using BiliLiveHelper.Bili;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace BiliLiveDanmaku.Modules
{
    public interface IModule
    {
        void Init(IModuleConfig config);
        IModuleConfig GetConfig();
        UIElement GetControl();
        void ProcessItem(BiliLiveJsonParser.IItem item);
        void Close();
    }
}
