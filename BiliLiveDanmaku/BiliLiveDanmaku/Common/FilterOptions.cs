using System;
using System.ComponentModel;

namespace BiliLiveDanmaku.Common
{
    [Serializable]
    public enum FilterOptions
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
        InteractEnter,
        [Description("关注直播间显示")]
        InteractFollow,
        [Description("分享直播间显示")]
        InteractShare,
        [Description("直播间禁言显示")]
        RoomBlock,
        [Description("弹幕播报")]
        DanmakuSpeech,
        [Description("醒目留言播报")]
        SuperChatSpeech,
        [Description("金瓜子礼物播报")]
        GoldenGiftSpeech,
        [Description("银瓜子礼物播报")]
        SilverGiftSpeech,
        [Description("礼物连击播报")]
        ComboSendSpeech,
    }

}
