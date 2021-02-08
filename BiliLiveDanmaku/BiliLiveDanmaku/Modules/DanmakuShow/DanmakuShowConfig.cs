using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;

namespace BiliLiveDanmaku.Modules
{
    [Serializable]
    public class DanmakuShowConfig : IModuleConfig
    {
        [Serializable]
        public enum DisplayFilterOptions
        {
            [Description("弹幕显示")]
            Danmaku,
            [Description("醒目留言显示")]
            SuperChat,
            [Description("金瓜子礼物显示")]
            GoldenGift,
            [Description("银瓜子礼物显示")]
            SilverGift,
            [Description("礼物连击显示")]
            ComboSend,
            [Description("节奏风暴显示")]
            RythmStorm,
            [Description("上舰显示")]
            GuardBuy,
            [Description("舰长欢迎显示")]
            WelcomeGuard,
            [Description("老爷欢迎显示")]
            Welcome,
            [Description("进入直播间显示")]
            InteractEntry,
            [Description("关注直播间显示")]
            InteractAttention,
            [Description("分享直播间显示")]
            InteractShare,
            [Description("特别关注直播间显示")]
            InteractSpecialAttention,
            [Description("互关直播间显示")]
            InteractMutualAttention,
            [Description("直播间禁言显示")]
            RoomBlock
        }

        public Rect WindowRect { get; set; }
        public Dictionary<DisplayFilterOptions, bool> OptionDict { get; set; }
        public bool IsLocked { get; set; }

        public DanmakuShowConfig()
        {
            OptionDict = new Dictionary<DisplayFilterOptions, bool>();
            foreach (DisplayFilterOptions filterOption in Enum.GetValues(typeof(DisplayFilterOptions)))
            {
                bool initValue = true;
                OptionDict.Add(filterOption, initValue);
            }
        }

        public DanmakuShowConfig(Dictionary<DisplayFilterOptions, bool> optionDict, Rect windowRect, bool isLocked)
        {
            OptionDict = optionDict;
            WindowRect = windowRect;
            IsLocked = isLocked;
        }

        public IModule CreateModule()
        {
            DanmakuShowModule displayModule = new DanmakuShowModule();
            displayModule.Init(this);
            return displayModule;
        }
    }
}
