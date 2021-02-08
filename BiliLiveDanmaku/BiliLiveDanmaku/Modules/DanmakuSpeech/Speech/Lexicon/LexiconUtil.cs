using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Resources;
using System.Xml.Linq;

namespace Speech.Lexicon
{
    public static class LexiconUtil
    {
        public static string Alphabet;
        public static List<Lexeme> Lexemes;

        public struct Lexeme
        {
            public string Grapheme;
            public string Alias;
            public string Phoneme;
            public Lexeme(string grapheme, string alias, string phoneme)
            {
                Grapheme = grapheme;
                Alias = alias;
                Phoneme = phoneme;
            }
        }

        static LexiconUtil()
        {
            Lexemes = new List<Lexeme>();

            Uri uri = new Uri("/Speech/Lexicon/Lexicon.xml", UriKind.Relative);
            StreamResourceInfo info = Application.GetResourceStream(uri);
            XDocument xDocument = XDocument.Load(info.Stream);
            Alphabet = (string)xDocument.Root.Attribute("alphabet");

            IEnumerable<XElement> lexemeEles = xDocument.Root.Elements("{http://www.w3.org/2005/01/pronunciation-lexicon}lexeme");
            foreach (XElement lexemeEle in lexemeEles)
            {
                XElement aliasEle = lexemeEle.Element("{http://www.w3.org/2005/01/pronunciation-lexicon}alias");
                string alias = aliasEle != null ? aliasEle.Value : null;
                XElement phonemeEle = lexemeEle.Element("{http://www.w3.org/2005/01/pronunciation-lexicon}phoneme");
                string phoneme = phonemeEle != null ? phonemeEle.Value : null;

                IEnumerable<XElement> graphemes = lexemeEle.Elements("{http://www.w3.org/2005/01/pronunciation-lexicon}grapheme");
                foreach (XElement graphemeEle in graphemes)
                {
                    string grapheme = graphemeEle.Value;
                    Lexeme lexeme = new Lexeme(grapheme, alias, phoneme);
                    Lexemes.Add(lexeme);
                }
            }
        }

        public static string MakeText(string text)
        {
            foreach (Lexeme lexeme in Lexemes)
            {
                Regex.IsMatch(text, lexeme.Grapheme);
                if (lexeme.Alias != null && lexeme.Phoneme != null)
                {
                    text = Regex.Replace(text, lexeme.Grapheme, $"<phoneme alphabet=\"{Alphabet}\" ph=\"{lexeme.Phoneme}\">{lexeme.Alias}</phoneme>");
                }
                else if (lexeme.Phoneme != null)
                {
                    text = Regex.Replace(text, lexeme.Grapheme, $"<phoneme alphabet=\"{Alphabet}\" ph=\"{lexeme.Phoneme}\">$0</phoneme>");
                }
                else if (lexeme.Alias != null)
                {
                    text = Regex.Replace(text, lexeme.Grapheme, lexeme.Alias);
                }
            }
            return text;
        }
    }
}
