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
}
