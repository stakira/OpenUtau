using System.Collections.Generic;
using WanaKanaNet;

namespace OpenUtau.Core {
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
        public override string Name => "Romaji to Hiragana";
        public override string Transform(string lyric) {
            return WanaKana.ToHiragana(lyric, new WanaKanaOptions() { CustomKanaMapping = mapping });
        }
    }

    public class HiraganaToRomajiTransformer : Transformer {
        public override string Name => "Hiragana to Romaji";
        public override string Transform(string lyric) {
            return WanaKana.ToRomaji(lyric);
        }
    }

    public class JapaneseVCVtoCVTransformer : Transformer {
        public override string Name => "Japanese VCV to CV";
        public override string Transform(string lyric) {
            if (lyric.Length > 2 && lyric[1] == ' ') {
                return lyric.Substring(2);
            } else {
                return lyric;
            }
        }
    }
}
