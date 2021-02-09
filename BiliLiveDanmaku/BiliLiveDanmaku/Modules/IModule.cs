using BiliLive;
using System.Windows;

namespace BiliLiveDanmaku.Modules
{
    public interface IModule
    {
        string Description { get; }
        IModuleConfig CreateDefaultConfig();
        void Init(IModuleConfig config);
        IModuleConfig GetConfig();
        UIElement GetControl();
        void ProcessItem(BiliLiveJsonParser.IItem item);
        void Close();
    }
}
