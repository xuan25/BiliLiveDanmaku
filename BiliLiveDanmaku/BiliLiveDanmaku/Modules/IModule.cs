using BiliLive;
using System.Windows;

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
