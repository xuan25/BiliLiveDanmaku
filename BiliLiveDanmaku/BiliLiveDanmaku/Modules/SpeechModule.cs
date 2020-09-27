using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Xml;
using BiliLiveDanmaku.Speech;
using BiliLiveDanmaku.Utils;
using BiliLiveHelper.Bili;
using Speech;

namespace BiliLiveDanmaku.Modules
{
    public class SpeechModule : IModule
    {
        SpeechControl Control { get; set; }
        public Dictionary<SpeechConfig.SpeechFilterOptions, bool> OptionDict { get; private set; }
        public int OutputDeviceId { get; private set; }
        public double Volume { get; private set; }

        GiftCacheManager giftCacheManager;
        SpeechProcessor speechProcessor;

        public void SetOutputDeviceId(int id)
        {
            OutputDeviceId = id;
            speechProcessor.OutputDeviceId = id;
        }

        public void SetVolume(double volume)
        {
            Volume = volume;
            speechProcessor.Volume = (float)volume;
        }

        public SpeechModule()
        {

        }

        public UserControl GetControl()
        {
            return Control;
        }

        public void Init(IModuleConfig config)
        {
            giftCacheManager = new GiftCacheManager(5);
            giftCacheManager.CacheExpired += GiftCacheManager_CacheExpired;
            speechProcessor = new SpeechProcessor();
            speechProcessor.QueueChanged += Synthesizer_QueueChanged;

            SpeechConfig speechConfig = (SpeechConfig)config;
            OptionDict = speechConfig.OptionDict;
            SetVolume(speechConfig.Volume);
            Control = new SpeechControl(this, speechConfig.OutputDevice);


            FileInfo fileInfo = new FileInfo("./config/speech.xml");
            Stream speechConfigStream = null;
            try
            {
                if (fileInfo.Exists)
                {
                    speechConfigStream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
                }
                else
                {
                    speechConfigStream = Application.GetResourceStream(new Uri("Config/Speech.xml", UriKind.RelativeOrAbsolute)).Stream;
                }
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.Load(speechConfigStream);
                XmlNode enableAttr = xmlDocument.SelectSingleNode("speech/@enable");
                string enable = enableAttr != null ? enableAttr.Value : "true";
                if (enable.ToLower() != "false")
                {
                    XmlNode tokenEndpointNode = xmlDocument.SelectSingleNode("speech/token_endpoint/text()");
                    XmlNode ttsEndpointNode = xmlDocument.SelectSingleNode("speech/tts_endpoint/text()");
                    XmlNode apiKeyNode = xmlDocument.SelectSingleNode("speech/api_key/text()");
                    string tokenEndpoint = tokenEndpointNode.Value;
                    string ttsEndpoint = ttsEndpointNode.Value;
                    string apiKey = apiKeyNode.Value;
                    speechProcessor.Init(tokenEndpoint, ttsEndpoint, apiKey);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                if (speechConfigStream != null)
                    speechConfigStream.Close();
            }
        }

        public void ClearQueue()
        {
            speechProcessor.ClearQueue();
        }

        private void Synthesizer_QueueChanged(object sender, int e)
        {
            Control.SetSynthesizeQueueCount(e);
        }

        private void GiftCacheManager_CacheExpired(object sender, GiftCacheManager.GiftCache e)
        {
            SpeakGift(e);
        }

        public IModuleConfig GetConfig()
        {
            SpeechConfig speechConfig = new SpeechConfig(OptionDict, Control.GetOutputDeviceName(), Volume);
            return speechConfig;
        }

        public void ProcessItem(BiliLiveJsonParser.IItem item)
        {
            switch (item.Cmd)
            {
                case BiliLiveJsonParser.Cmds.DANMU_MSG:
                    BiliLiveJsonParser.Danmaku danmaku = (BiliLiveJsonParser.Danmaku)item;
                    if (danmaku.Type == 0)
                    {
                        if (OptionDict[SpeechConfig.SpeechFilterOptions.DanmakuSpeech])
                            SpeakDanmaku(danmaku);
                    }
                    break;
                case BiliLiveJsonParser.Cmds.SUPER_CHAT_MESSAGE:
                    BiliLiveJsonParser.SuperChat superChat = (BiliLiveJsonParser.SuperChat)item;
                    if (OptionDict[SpeechConfig.SpeechFilterOptions.SuperChatSpeech])
                        SpeakSuperChat(superChat);
                    break;
                case BiliLiveJsonParser.Cmds.SEND_GIFT:
                    BiliLiveJsonParser.Gift gift = (BiliLiveJsonParser.Gift)item;
                    if (!giftCacheManager.AppendToExist(gift))
                    {
                        giftCacheManager.AppendCache(gift);
                    }
                    break;
                case BiliLiveJsonParser.Cmds.COMBO_SEND:
                    BiliLiveJsonParser.ComboSend comboSend = (BiliLiveJsonParser.ComboSend)item;
                    if (OptionDict[SpeechConfig.SpeechFilterOptions.ComboSendSpeech])
                        SpeakComboSend(comboSend);
                    break;
            }
        }

        public void Close()
        {
            
        }

        private void SpeakDanmaku(BiliLiveJsonParser.Danmaku item)
        {
            if (speechProcessor.IsAvalable)
            {
                string ssmlDoc = SsmlHelper.Danmaku(item.Sender.Name, item.Message);
                speechProcessor.Speak(ssmlDoc);
            }
        }

        private void SpeakSuperChat(BiliLiveJsonParser.SuperChat item)
        {
            if (speechProcessor.IsAvalable)
            {
                string ssmlDoc = SsmlHelper.SuperChat(item.User.Name, item.Message);
                speechProcessor.Speak(ssmlDoc);
            }
        }

        private void SpeakGift(GiftCacheManager.GiftCache gift)
        {
            if (gift.CoinType == "gold" && !OptionDict[SpeechConfig.SpeechFilterOptions.GoldenGiftSpeech])
                return;
            if (gift.CoinType == "silver" && !OptionDict[SpeechConfig.SpeechFilterOptions.SilverGiftSpeech])
                return;

            if (speechProcessor.IsAvalable)
            {
                string ssmlDoc = SsmlHelper.Gift(gift.Username, gift.Number, gift.GiftName, gift.Action);
                speechProcessor.Speak(ssmlDoc);
            }
        }

        private void SpeakComboSend(BiliLiveJsonParser.ComboSend item)
        {
            if (speechProcessor.IsAvalable)
            {
                string ssmlDoc = SsmlHelper.Gift(item.Sender.Name, item.Number, item.GiftName, item.Action);
                speechProcessor.Speak(ssmlDoc);
            }
        }

        
    }
}
