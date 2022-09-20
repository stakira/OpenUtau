using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace OpenUtau.Core.Util {
    public static class SplitLyrics {
        static Regex whitespace = new Regex(@"\s");
        static Regex standalone = new Regex(
            @"\p{IsCJKUnifiedIdeographs}|\p{IsHiragana}|\p{IsKatakana}|\p{IsHangulSyllables}");

        public static List<string> Split(string text) {
            var lyrics = new List<string>();
            var builder = new StringBuilder();
            var etor = StringInfo.GetTextElementEnumerator(text);
            while (etor.MoveNext()) {
                string ele = etor.GetTextElement();
                if (whitespace.IsMatch(ele)) {
                    if (builder.Length > 0) {
                        lyrics.Add(builder.ToString());
                        builder.Clear();
                    }
                } else if (standalone.IsMatch(ele)) {
                    if (builder.Length > 0) {
                        lyrics.Add(builder.ToString());
                        builder.Clear();
                    }
                    lyrics.Add(ele);
                } else if (ele == "\"") {
                    while (etor.MoveNext()) {
                        string ele1 = etor.GetTextElement();
                        if (ele1 == "\"") {
                            lyrics.Add(builder.ToString());
                            builder.Clear();
                            break;
                        } else {
                            builder.Append(ele1);
                        }
                    }
                } else {
                    builder.Append(ele);
                }
            }
            if (builder.Length > 0) {
                lyrics.Add(builder.ToString());
                builder.Clear();
            }
            return lyrics;
        }

        public static string Join(IEnumerable<string> lyrics) {
            var builder = new StringBuilder();
            foreach (string lyric in lyrics) {
                if (builder.Length != 0) {
                    builder.Append($" ");
                }
                if (lyric.Length == 0) {
                    builder.Append("\"\"");
                } else if (whitespace.IsMatch(lyric)) {
                    builder.Append($"\"{lyric}\"");
                } else if (standalone.IsMatch(lyric)) {
                    if (lyric.Length == 1) {
                        builder.Append(lyric);
                    } else {
                        builder.Append($"\"{lyric}\"");
                    }
                } else {
                    builder.Append($"{lyric}");
                }
            }
            return builder.ToString();
        }
    }
}
