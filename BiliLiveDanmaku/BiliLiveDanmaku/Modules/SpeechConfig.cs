using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BiliLiveDanmaku.Modules
{
    [Serializable]
    public class SpeechConfig : IModuleConfig
    {
        [Serializable]
        public enum SpeechFilterOptions
        {
            [Description("弹幕播报")]
            DanmakuSpeech,
            [Description("醒目留言播报")]
            SuperChatSpeech,
            [Description("金瓜子礼物播报")]
            GoldenGiftSpeech,
            [Description("银瓜子礼物播报")]
            SilverGiftSpeech,
            [Description("礼物连击播报")]
            ComboSendSpeech
        }

        public Dictionary<SpeechFilterOptions, bool> OptionDict { get; set; }
        public string OutputDevice { get; set; }
        public double Volume { get; set; }

        public SpeechConfig()
        {
            OutputDevice = string.Empty;
            Volume = 1;

            OptionDict = new Dictionary<SpeechFilterOptions, bool>();
            foreach (SpeechFilterOptions filterOption in Enum.GetValues(typeof(SpeechFilterOptions)))
            {
                bool initValue = true;
                OptionDict.Add(filterOption, initValue);
            }
        }

        public SpeechConfig(Dictionary<SpeechFilterOptions, bool> optionDict, string outputDevice, double volume)
        {
            OptionDict = optionDict;
            OutputDevice = outputDevice;
            Volume = volume;
        }

        public IModule CreateModule()
        {
            SpeechModule speechModule = new SpeechModule();
            speechModule.Init(this);
            return speechModule;
        }

    }
}
