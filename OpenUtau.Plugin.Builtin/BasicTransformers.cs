using System.Collections.Generic;
using WanaKanaNet;
using OpenUtau.Api;

namespace OpenUtau.Plugin.Builtin {
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

    [Transformer("Hiragana to Romaji")]
    public class HiraganaToRomajiTransformer : Transformer {
        public override string Transform(string lyric) {
            return WanaKana.ToRomaji(lyric);
        }
    }

    [Transformer("Japanese VCV to CV")]
    public class JapaneseVCVtoCVTransformer : Transformer {
        public override string Transform(string lyric) {
            if (lyric.Length > 2 && lyric[1] == ' ') {
                return lyric.Substring(2);
            } else {
                return lyric;
            }
        }
    }
}
