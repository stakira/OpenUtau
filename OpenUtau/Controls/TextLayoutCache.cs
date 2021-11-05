using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;

namespace OpenUtau.App.Controls {
    static class TextLayoutCache {
        private static readonly Dictionary<Tuple<string, IBrush, double>, TextLayout> cache
            = new Dictionary<Tuple<string, IBrush, double>, TextLayout>();

        public static void Clear() {
            cache.Clear();
        }

        public static TextLayout Get(string text, IBrush brush, double fontSize) {
            var key = Tuple.Create(text, brush, fontSize);
            if (!cache.TryGetValue(key, out var textLayout)) {
                textLayout = new TextLayout(
                    text,
                    new Typeface(FontFamily.Default),
                    fontSize,
                    brush,
                    TextAlignment.Left,
                    TextWrapping.NoWrap);
                cache.Add(key, textLayout);
            }
            return textLayout;
        }
    }
}
