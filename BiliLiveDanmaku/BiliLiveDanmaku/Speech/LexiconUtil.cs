using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Resources;
using System.Xml.Linq;

namespace BiliLiveDanmaku.Speech
{
    public static class LexiconUtil
    {
        public static string Alphabet;
        public static Dictionary<string, AliasAndPhoneme> LexemeDict;

        public struct AliasAndPhoneme
        {
            public string Alias;
            public string Phoneme;

            public AliasAndPhoneme(string alias, string phoneme)
            {
                Alias = alias;
                Phoneme = phoneme;
            }
        }

        static LexiconUtil()
        {
            LexemeDict = new Dictionary<string, AliasAndPhoneme>();

            Uri uri = new Uri("/Speech/Lexicon.xml", UriKind.Relative);
            StreamResourceInfo info = Application.GetResourceStream(uri);
            XDocument xDocument = XDocument.Load(info.Stream);
            Alphabet = (string)xDocument.Root.Attribute("alphabet");

            IEnumerable<XElement> lexemes = xDocument.Root.Elements("{http://www.w3.org/2005/01/pronunciation-lexicon}lexeme");
            foreach (XElement lexeme in lexemes)
            {
                XElement aliasEle = lexeme.Element("{http://www.w3.org/2005/01/pronunciation-lexicon}alias");
                string alias = aliasEle != null ? aliasEle.Value : null;
                XElement phonemeEle = lexeme.Element("{http://www.w3.org/2005/01/pronunciation-lexicon}phoneme");
                string phoneme = phonemeEle != null ? phonemeEle.Value : null;
                AliasAndPhoneme aliasAndPhoneme = new AliasAndPhoneme(alias, phoneme);

                IEnumerable<XElement> graphemes = lexeme.Elements("{http://www.w3.org/2005/01/pronunciation-lexicon}grapheme");
                foreach (XElement graphemeEle in graphemes)
                {
                    string grapheme = graphemeEle.Value;
                    LexemeDict.Add(grapheme, aliasAndPhoneme);
                }
            }
        }

        public static string MakeText(string text)
        {
            foreach(KeyValuePair<string, AliasAndPhoneme> lexeme in LexemeDict)
            {
                Regex.IsMatch(text, lexeme.Key);
                if (lexeme.Value.Alias != null && lexeme.Value.Phoneme != null)
                {
                    text = Regex.Replace(text, lexeme.Key, $"<phoneme alphabet=\"{Alphabet}\" ph=\"{lexeme.Value.Phoneme}\">{lexeme.Value.Alias}</phoneme>");
                }
                else if (lexeme.Value.Phoneme != null)
                {
                    text = Regex.Replace(text, lexeme.Key, $"<phoneme alphabet=\"{Alphabet}\" ph=\"{lexeme.Value.Phoneme}\">$0</phoneme>");
                }
                else if (lexeme.Value.Alias != null)
                {
                    text = Regex.Replace(text, lexeme.Key, lexeme.Value.Alias);
                }
            }
            return text;
        }
    }
}
