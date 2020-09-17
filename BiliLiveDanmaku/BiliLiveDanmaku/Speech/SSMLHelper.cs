using Speech.Lang;
using Speech.Lexicon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Resources;

namespace BiliLiveDanmaku.Speech
{
    public static class SsmlHelper
    {
        
        public static string Danmaku(string user, string message)
        {
            string speechMessage = LexiconUtil.MakeText(SecurityElement.Escape(message));

            string templateUri = "/Speech/Template/Danmaku.xml";
            LangDetection.Language language = LangDetection.Detect(speechMessage);
            switch (language)
            {
                case LangDetection.Language.Japanese:
                    templateUri = "/Speech/Template/ja-JP/Danmaku.xml";
                    break;
                case LangDetection.Language.Chinese:
                    templateUri = "/Speech/Template/zh-CN/Danmaku.xml";
                    break;
                case LangDetection.Language.English:
                    templateUri = "/Speech/Template/en-US/Danmaku.xml";
                    break;
            }

            string template = GetTextFromResource(templateUri);
            string ssmlDoc = template.Replace("{User}", SecurityElement.Escape(user)).Replace("{Message}", speechMessage);
            return ssmlDoc;
        }

        public static string SuperChat(string user, string message)
        {
            string speechMessage = LexiconUtil.MakeText(SecurityElement.Escape(message));

            string templateUri = "/Speech/Template/SuperChat.xml";
            LangDetection.Language language = LangDetection.Detect(speechMessage);
            switch (language)
            {
                case LangDetection.Language.Japanese:
                    templateUri = "/Speech/Template/ja-JP/SuperChat.xml";
                    break;
                case LangDetection.Language.Chinese:
                    templateUri = "/Speech/Template/zh-CN/SuperChat.xml";
                    break;
                case LangDetection.Language.English:
                    templateUri = "/Speech/Template/en-US/SuperChat.xml";
                    break;
            }

            string template = GetTextFromResource(templateUri);
            string ssmlDoc = template.Replace("{User}", SecurityElement.Escape(user)).Replace("{Message}", speechMessage);
            return ssmlDoc;
        }

        public static string Gift(string user, uint count, string giftName)
        {
            string templateUri = "/Speech/Template/Gift.xml";
            string template = GetTextFromResource(templateUri);
            string ssmlDoc = template.Replace("{User}", SecurityElement.Escape(user)).Replace("{Count}", count.ToString()).Replace("{Gift}", SecurityElement.Escape(giftName));
            return ssmlDoc;
        }


        private static Dictionary<string, string> TextDict = new Dictionary<string, string>();
        public static string GetTextFromResource(string uri)
        {
            if (TextDict.ContainsKey(uri))
            {
                return TextDict[uri];
            }
            else
            {
                StreamResourceInfo info = Application.GetResourceStream(new Uri(uri, UriKind.RelativeOrAbsolute));
                using (StreamReader streamReader = new StreamReader(info.Stream))
                {
                    string text = streamReader.ReadToEnd();
                    TextDict[uri] = text;
                    return text;
                }
            }
            
        }
    }
}
