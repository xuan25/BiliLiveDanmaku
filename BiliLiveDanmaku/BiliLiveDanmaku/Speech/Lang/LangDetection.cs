using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Speech.Lang
{
    public static class LangDetection
    {
        public enum Language
        {
            Unknow,
            English,
            Chinese,
            Japanese,
            Korean
        }

        public static Language Detect(string s)
        {
            int engCount = 0;
            int chiCount = 0;
            int japCount = 0;
            int korCount = 0;

            for (int i = 0; i < s.Length; i++)
            {
                Language language = Detect(s[i]);
                switch (language)
                {
                    case Language.Korean:
                        korCount++;
                        break;
                    case Language.Japanese:
                        japCount++;
                        break;
                    case Language.Chinese:
                        chiCount++;
                        break;
                    case Language.English:
                        engCount++;
                        break;
                }
            }

            if (korCount > 0)
            {
                return Language.Korean;
            }
            if (japCount > 0)
            {
                return Language.Japanese;
            }
            if (chiCount > 0)
            {
                return Language.Chinese;
            }
            if (engCount > 0)
            {
                return Language.English;
            }
            return Language.Unknow;
        }

        public static Language Detect(char c)
        {
            if (InKorean(c))
            {
                return Language.Korean;
            }
            if (InJapanese(c))
            {
                return Language.Japanese;
            }
            if (InCJK(c))
            {
                return Language.Chinese;
            }
            if (InEnglish(c))
            {
                return Language.English;
            }
            return Language.Unknow;
        }

        public static bool InEnglish(char c)
        {
            // Basic Latin (Partial)
            if (c >= 0x0041 && c <= 0x005A)
            {
                return true;
            }
            return false;
        }

        public static bool InCJK(char c)
        {
            // CJK Unified Ideographs
            if (c >= 0x4E00 && c <= 0x9FFF)
            {
                return true;
            }
            // CJK Unified Ideographs Extension A
            if (c >= 0x3400 && c <= 0x4DBF)
            {
                return true;
            }
            // CJK Unified Ideographs Extension B
            if (c >= 0x20000 && c <= 0x2A6DF)
            {
                return true;
            }
            // CJK Unified Ideographs Extension C
            if (c >= 0x2A700 && c <= 0x2B73F)
            {
                return true;
            }
            // CJK Unified Ideographs Extension D
            if (c >= 0x2B740 && c <= 0x2B81F)
            {
                return true;
            }
            // CJK Unified Ideographs Extension E
            if (c >= 0x2B820 && c <= 0x2CEAF)
            {
                return true;
            }
            // CJK Unified Ideographs Extension F
            if (c >= 0x2CEB0 && c <= 0x2EBEF)
            {
                return true;
            }
            return false;
        }

        public static bool InJapanese(char c)
        {
            // Hiragana
            if (c >= 0x3040 && c <= 0x309F)
            {
                return true;
            }
            // Katakana
            if (c >= 0x30A0 && c <= 0x30FF)
            {
                return true;
            }
            // Katakana Phonetic Extensions
            if (c >= 0x31F0 && c <= 0x31FF)
            {
                return true;
            }
            return false;
        }

        public static bool InKorean(char c)
        {
            // Hangul Syllables
            if (c >= 0xAC00 && c <= 0xD7AF)
            {
                return true;
            }
            // Hangul Jamo Extended-A
            if (c >= 0xA960 && c <= 0xA97F)
            {
                return true;
            }
            // Hangul Jamo Extended-B
            if (c >= 0xD7B0 && c <= 0xD7FF)
            {
                return true;
            }
            return false;
        }

    }
}
