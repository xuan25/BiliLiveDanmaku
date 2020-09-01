using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Resources;

namespace BiliLiveDanmaku.Speech
{
    public static class TemplateManager
    {
        private static string _danmakuSpeechTemplate = null;
        public static string DanmakuSpeechTemplate
        {
            get
            {
                if (_danmakuSpeechTemplate != null)
                    return _danmakuSpeechTemplate;

                Uri uri = new Uri("/Speech/Template/Danmaku.xml", UriKind.Relative);
                StreamResourceInfo info = Application.GetResourceStream(uri);
                using (StreamReader streamReader = new StreamReader(info.Stream))
                {
                    string template = streamReader.ReadToEnd();
                    _danmakuSpeechTemplate = template;
                    return template;
                }
            }
        }

        private static string _giftSpeechTemplate = null;
        public static string GiftSpeechTemplate
        {
            get
            {
                if (_giftSpeechTemplate != null)
                    return _giftSpeechTemplate;

                Uri uri = new Uri("/Speech/Template/Gift.xml", UriKind.Relative);
                StreamResourceInfo info = Application.GetResourceStream(uri);
                using (StreamReader streamReader = new StreamReader(info.Stream))
                {
                    string template = streamReader.ReadToEnd();
                    _giftSpeechTemplate = template;
                    return template;
                }
            }
        }

        private static string _superChatTemplate = null;
        public static string SuperChatTemplate
        {
            get
            {
                if (_superChatTemplate != null)
                    return _superChatTemplate;

                Uri uri = new Uri("/Speech/Template/SuperChat.xml", UriKind.Relative);
                StreamResourceInfo info = Application.GetResourceStream(uri);
                using (StreamReader streamReader = new StreamReader(info.Stream))
                {
                    string template = streamReader.ReadToEnd();
                    _superChatTemplate = template;
                    return template;
                }
            }
        }
    }
}
