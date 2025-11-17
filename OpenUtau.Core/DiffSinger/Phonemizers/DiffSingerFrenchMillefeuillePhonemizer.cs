using System;
using System.Collections.Generic;
using System.Linq;
using OpenUtau.Api;
using OpenUtau.Core.G2p;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Core.DiffSinger {
    [Phonemizer("DiffSinger French Millefeuille Phonemizer", "DIFFS FR MILLE", language: "FR")]
    public class DiffSingerFrenchMillfeuillePhonemizer : DiffSingerG2pPhonemizer {
        readonly HashSet<string> liaisonConsonants = new HashSet<string> {
            "s", "z", "t", "d", "n", "ng", "f", "v", "r",
        };

        readonly HashSet<string> mandatoryLiaisonWords = new HashSet<string> {
        "et", "accessit", "affidavit", "audit", "azimut", "brut", "cajeput",
        "cet", "chut", "coït", "comput", "déficit", "diktat", "dot", "exeat",
        "exit", "fiat", "granit", "huit", "incipit", "introït", "inuit",
        "kumquat", "lut", "magnificat", "mat", "mazout", "net", "obit",
        "occiput", "pat", "prétérit", "prurit", "rut", "satisfecit",
        "scorbut", "sinciput", "transat", "transit", "ut", "zut",
        };

        protected override string GetDictionaryName() => "dsdict-fr-millefeuille.yaml";
        public override string GetLangCode() => "fr";
        protected override IG2p LoadBaseG2p() => new FrenchMillefeuilleG2p();

        protected override string[] GetBaseG2pVowels() => new string[] {
            "ah", "eh", "ae", "ee", "oe", "ih", "oh", "oo", "ou", "uh", "en", "in", "on",
        };

        protected override string[] GetBaseG2pConsonants() => new string[] {
            "y", "w", "f", "k", "p", "s", "sh", "t", "h", "b", "d", "g", "l",
            "m", "n", "r", "v", "z", "j", "ng", "q", "uy", "vf", "cl",
        };

        string NormalizeWord(string lyric) {
            if (string.IsNullOrEmpty(lyric)) {
                return string.Empty;
            }
            lyric = lyric.Trim();
            if (lyric.EndsWith("*", StringComparison.Ordinal)) {
                lyric = lyric.Substring(0, lyric.Length - 1).Trim();
            }
            return lyric.ToLowerInvariant();
        }

        bool IsMandatoryLiaisonWord(string lyric) {
            var norm = NormalizeWord(lyric);
            if (string.IsNullOrEmpty(norm)) {
                return false;
            }
            return mandatoryLiaisonWords.Contains(norm);
        }

        string GetBaseSymbol(string symbol) {
            if (string.IsNullOrEmpty(symbol)) {
                return symbol;
            }
            var i = symbol.LastIndexOf('/');
            if (i >= 0 && i < symbol.Length - 1) {
                return symbol.Substring(i + 1);
            }
            return symbol;
        }

        protected override string[] PostProcessWordSymbols(Note[][] phrase, int wordIndex, string[] symbols) {
            if (symbols == null || symbols.Length == 0) {
                return symbols;
            }
            var wordNotes = phrase[wordIndex];
            if (wordNotes == null || wordNotes.Length == 0) {
                return symbols;
            }

            var firstNote = wordNotes[0];
            if (!string.IsNullOrEmpty(firstNote.phoneticHint)) {
                return symbols;
            }

            var rawLyric = firstNote.lyric ?? string.Empty;
            var hasStar = rawLyric.EndsWith("*", StringComparison.Ordinal);

            var norm = NormalizeWord(rawLyric);
            if (string.IsNullOrEmpty(norm)) {
                return symbols;
            }
            var lastChar = norm[norm.Length - 1];
            const string consonantLetters = "bcdfghjklmnpqrstvwxyzç";
            var spellingEndsWithConsonant = consonantLetters.IndexOf(char.ToLowerInvariant(lastChar)) >= 0;

            var lastSymbol = symbols[symbols.Length - 1];
            var baseSymbol = GetBaseSymbol(lastSymbol);
            if (!spellingEndsWithConsonant || !liaisonConsonants.Contains(baseSymbol)) {
                return symbols;
            }

            string nextLyric = string.Empty;
            if (wordIndex + 1 < phrase.Length) {
                var nextNotes = phrase[wordIndex + 1];
                if (nextNotes != null && nextNotes.Length > 0) {
                    nextLyric = nextNotes[0].lyric ?? string.Empty;
                }
            }
            bool nextStartsWithVowel = false;
            if (!string.IsNullOrEmpty(nextLyric)) {
                var tmp = nextLyric.Trim();
                if (tmp.EndsWith("*", StringComparison.Ordinal)) {
                    tmp = tmp.Substring(0, tmp.Length - 1).Trim();
                }
                if (tmp.Length > 0) {
                    var c = char.ToLowerInvariant(tmp[0]);
                    const string vowels = "aeiouàâäéèêëîïôöùûüÿœ";
                    nextStartsWithVowel = vowels.IndexOf(c) >= 0;
                }
            }

            var keepFinal = IsMandatoryLiaisonWord(rawLyric);
            if (!keepFinal && nextStartsWithVowel) {
                keepFinal = true;
            }
            if (hasStar) {
                keepFinal = !keepFinal;
            }

            if (keepFinal || symbols.Length == 1) {
                return symbols;
            }

            var trimmed = new string[symbols.Length - 1];
            Array.Copy(symbols, trimmed, symbols.Length - 1);
            return trimmed;
        }
    }
}
