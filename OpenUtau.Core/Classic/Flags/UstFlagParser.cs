
using System;
using System.Collections.Generic;

namespace OpenUtau.Classic.Flags {
    public class UstFlagParser {
        public IList<UstFlag> Parse(string text) {
            List<UstFlag> flags = new List<UstFlag>();

            int currentIndex = 0;
            while (currentIndex < text.Length) {
                string key = ExtractKey(text, ref currentIndex);
                int value = ExtractValue(text, ref currentIndex);
                flags.Add(new UstFlag(key, value));
            }
            return flags;
        }

        private string ExtractKey(string text, ref int currentIndex) {
            string key = string.Empty;

            if (IsSingleCharacterFlag(text[currentIndex])) {
                key += text[currentIndex];
                currentIndex++;
                return key;
            }
            while (currentIndex < text.Length && Char.IsLetter(text[currentIndex])) {
                key += text[currentIndex];
                currentIndex++;
            }
            return key;
        }

        private int ExtractValue(string text, ref int currentIndex) {
            if (currentIndex >= text.Length) {
                return 0;
            }
            int value = 0;
            bool negative = IsNegative(text[currentIndex], ref currentIndex);

            while (currentIndex < text.Length && Char.IsDigit(text[currentIndex])) {
                value = (value * 10) + (text[currentIndex] - '0');
                currentIndex++;
            }
            return negative ? -value : value;
        }

        private bool IsNegative(char input, ref int currentIndex) {
            if (input == '-') {
                currentIndex++;
                return true;
            }
            if (input == '+') {
                currentIndex++;
            }
            return false;
        }

        private bool IsSingleCharacterFlag(char input) {
            var patterns = new HashSet<char> { 'N' };
            return patterns.Contains(input);
        }
    }
}
