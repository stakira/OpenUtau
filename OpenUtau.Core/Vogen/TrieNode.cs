using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace OpenUtau.Core.Vogen {
    class TrieNode {
        public Dictionary<string, TrieNode> children = new Dictionary<string, TrieNode>();
        public string[] pinyins;

        public static TrieNode LoadDictionary(IEnumerable<string> lines) {
            TrieNode root = new TrieNode();
            foreach (var line in lines.Skip(1)) {
                var parts = line.Trim().Split(',');
                var etor = StringInfo.GetTextElementEnumerator(parts[0]);
                BuildTrie(root, etor, parts.Skip(1).ToArray());
            }
            return root;
        }

        private static void BuildTrie(TrieNode node, TextElementEnumerator etor, string[] pinyins) {
            if (!etor.MoveNext()) {
                node.pinyins = pinyins;
                return;
            }
            string hanzi = etor.GetTextElement();
            if (!node.children.TryGetValue(hanzi, out var child)) {
                node.children[hanzi] = child = new TrieNode();
            }
            BuildTrie(child, etor, pinyins);
        }

        public string[] Query(string s) {
            var etor = StringInfo.GetTextElementEnumerator(s);
            string[] pinyins = null;
            while (etor.MoveNext() && children.TryGetValue(etor.GetTextElement(), out var node)) {
                if (node.pinyins != null) {
                    pinyins = node.pinyins;
                }
            }
            return pinyins;
        }
    }
}
