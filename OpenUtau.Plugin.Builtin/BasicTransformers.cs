using System.Collections.Generic;
using WanaKanaNet;
using OpenUtau.Api;

namespace OpenUtau.Plugin.Builtin {
    /// <summary>
    /// A Transformer that uses WanaKanaNet to convert romaji to hiragana.
    /// </summary>
    [Transformer("Romaji to Hiragana")]
    public class RomajiToHiraganaTransformer : Transformer {
        static Dictionary<string, string> mapping = new Dictionary<string, string>() {
            {".", "."},
            {",", ","},
            {":", ":"},
            {"/", "/"},
            {"!", "!"},
            {"?", "?"},
            {"~", "~"},
            {"-", "-"},
            {"‘", "‘"},
            {"’", "’"},
            {"“", "“"},
            {"”", "”"},
            {"[", "["},
            {"]", "]"},
            {"(", "("},
            {")", ")"},
            {"{", "{"},
            {"}", "}"},
        };
        public override string Transform(string lyric) {
            return WanaKana.ToHiragana(lyric, new WanaKanaOptions() { CustomKanaMapping = mapping });
        }
    }

    /// <summary>
    /// A Transformer that uses WanaKanaNet to convert hiragana to romaji.
    /// </summary>
    [Transformer("Hiragana to Romaji")]
    public class HiraganaToRomajiTransformer : Transformer {
        public override string Transform(string lyric) {
            return WanaKana.ToRomaji(lyric);
        }
    }

    /// <summary>
    /// A Transformer that converts Japanese VCV to CV.
    /// </summary>
    [Transformer("Japanese VCV to CV")]
    public class JapaneseVCVtoCVTransformer : Transformer {
        public override string Transform(string lyric) {
            if (lyric.Length > 2 && lyric[1] == ' ') {
                // When the lyric is like "a あ", "a R" or "- あ", cut off the first two characters.
                return lyric.Substring(2);
            } else {
                // Otherwise cannot recognize VCV, return as is.
                return lyric;
            }
        }
    }

    /// <summary>
    /// A Transformer that removes tone suffix like "C4" from lyric.
    /// </summary>
    [Transformer("Remove Tone Suffix")]
    public class RemoveToneSuffixTransformer : Transformer {
        public override string Transform(string lyric) {
            if (lyric.Length <= 2) {
                return lyric;
            }
            string last2 = lyric.Substring(lyric.Length - 2);
            if (last2[0] >= 'A' && last2[0] <= 'G' && last2[1] >= '0' && last2[1] <= '9') {
                return lyric.Substring(0, lyric.Length - 2);
            }
            return lyric;
        }
    }

    /// <summary>
    /// A Transformer that simply removes an english letter from end of lyric.
    /// </summary>
    [Transformer("Remove Letter Suffix")]
    public class RemoveLetterSuffixTransformer : Transformer {
        public override string Transform(string lyric) {
            if (lyric.Length <= 2) {
                return lyric;
            }
            var c = lyric[lyric.Length - 1];
            if ((c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z') && c != 'R' && c != 'r') {
                return lyric.Substring(0, lyric.Length - 1);
            }
            return lyric;
        }
    }
}
