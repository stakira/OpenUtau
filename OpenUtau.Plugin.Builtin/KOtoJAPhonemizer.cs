using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using OpenUtau.Api;
using OpenUtau.Classic;
using OpenUtau.Core.Editing;
using OpenUtau.Core.Ustx;
using SharpGen.Runtime;
using WanaKanaNet;
using static OpenUtau.Api.Phonemizer;

namespace OpenUtau.Plugin.Builtin {

    [Phonemizer("Korean to Japanese Phonemizer", "KO to JA", "Lotte V", language: "KO")]

    /// <summary>
    /// A phonemizer to use Japanese banks on Korean USTx files.
    /// TODO: Support more than just VCV.
    /// </summary>

    public class KOtoJAPhonemizer : Phonemizer {
        /// <summary>
		/// Initial jamo as ordered in Unicode
		/// </summary>
		static readonly string[] initials = { "g", "k", "n", "d", "t", "r", "m", "b", "p", "s", "s", string.Empty, "z", "ts", "ts", "k", "t", "p", "h" };
        // ㄱ ㄲ ㄴ ㄷ ㄸ ㄹ ㅁ ㅂ ㅃ ㅅ ㅆ ㅇ ㅈ ㅉ ㅊ ㅋ ㅌ ㅍ ㅎ

        /// <summary>
		/// Medial jamo as ordered in Unicode
		/// </summary>
		static readonly string[] medials = { "a", "e", "ya", "ye", "o", "e", "yo", "ye", "o", "wa", "we", "we", "yo", "u", "wo", "we", "wi", "yu", "u", "ui", "i" };
        // ㅏ ㅐ ㅑ ㅒ ㅓ ㅔ ㅕ ㅖ ㅗ ㅘ ㅙ ㅚ ㅛ ㅜ ㅝ ㅞ ㅟ ㅠ ㅡ ㅢ ㅣ

        /// <summary>
		/// Final jamo as ordered in Unicode + vowel end breath sounds (inhale and exhale)
		/// </summary>
		static readonly string[] finals = { string.Empty, "k", "k", "k", "n", "n", "n", "t", "r", "r", "r", "r", "r", "r", "r", "r", "m", "p", "p", "t", "t", "n", "t", "t", "k", "t", "p", "t", string.Empty, string.Empty, string.Empty, };
        // - ㄱ ㄲ ㄳ ㄴ ㄵ ㄶ ㄷ ㄹ ㄺ ㄻ ㄼ ㄽ ㄾ ㄿ ㅀ ㅁ ㅂ ㅄ ㅅ ㅆ ㅇ ㅈ ㅊ ㅋ ㅌ ㅍ ㅎ H B bre

        /// <summary>
        /// Sonorant batchim (i.e., extendable batchim sounds)
        /// </summary>
        static readonly string[] sonorants = { "n", "n" };

        /// <summary>
        /// Non-extendable sonorants
        /// </summary>
        static readonly string[] otherSonorants = { "m", "r" };

        /// <summary>
		/// Extra English-based sounds for phonetic hint input + alternate romanizations for tense plosives (ㄲ, ㄸ, ㅃ)
		/// </summary>
		static readonly string[] extras = { "f", "v", "th", "dh", "z", "rr", "kk", "pp", "tt", "l" };

        /// <summary>
        /// Plain vowels for VC notes.
        /// </summary>
        static readonly string[] plainVowels = { "a", "i", "u", "e", "o", "n" };

        /// <summary>
		/// Gets the romanized initial, medial, and final components of the passed Hangul syllable.
		/// </summary>
		/// <param name="syllable">A Hangul syllable.</param>
		/// <returns>An array containing the initial, medial, and final sounds of the syllable.</returns>
        public string[] GetIMF(string syllable) {
            byte[] bytes = Encoding.Unicode.GetBytes(syllable);
            int numval = Convert.ToInt32(bytes[0]) + Convert.ToInt32(bytes[1]) * (16 * 16);
            numval -= 44032;
            int i = numval / 588;
            numval -= i * 588;
            int m = numval / 28;
            numval -= m * 28;
            int f = numval;

            string[] ret = { initials[i], medials[m], finals[f] };

            return ret;
        }

        /// <summary>
		/// Separates the initial, medial, and final components of the passed phonetic hint.
		/// </summary>
		/// <param name="hint">A phonetic hint.</param>
		/// <returns>An array containing the initial, medial, and final sounds of the phonetic hint.</returns>
		public string[] GetIMFFromHint(string hint) {
            string[] hintSplit = hint.Split(' ');

            string i = Array.IndexOf(initials.Concat(extras).ToArray(), hintSplit[0]) > -1 ? hintSplit[0] : string.Empty;
            string m = string.IsNullOrEmpty(i) ? hintSplit[0] : hintSplit[1];
            string f = (hintSplit.Length > 2 || (hintSplit.Length == 2 && string.IsNullOrEmpty(i))) ? hintSplit[^1] : string.Empty;

            if (m == "ui") m = "u".Substring(1) + "i".Substring(2);

            string[] ret = { i, m, f };

            return ret;
        }

        /// <summary>
		/// Separates the initial, medial, and final components of the passed phonetic hint.
		/// </summary>
		/// <param name="hint">A phonetic hint.</param>
		/// <returns>An array containing the initial, medial, and final sounds of the phonetic hint.</returns>
        public string GetLastSoundOfAlias(string lyric) {
            string lastSound = lyric.Split(' ')[^1];
            Regex symbolRemove = new Regex(@"\W");
            MatchCollection symbolMatches = symbolRemove.Matches(lastSound);

            foreach (Match symbolMatch in symbolMatches) {
                lastSound = lastSound.Replace(symbolMatch.Value, string.Empty);
            }

            if (Array.IndexOf(finals, lastSound) == -1) {
                foreach (string i in initials) {
                    if (!string.IsNullOrEmpty(i)) lastSound = lastSound.Replace(i, string.Empty);
                }
            }

            return lastSound;
        }

        private USinger singer;

        // Store singer
        public override void SetSinger(USinger singer) => this.singer = singer;

        // Legacy mapping. Might adjust later to new mapping style.
        public override bool LegacyMapping => true;

        private Dictionary<string, string> StartingConsonant => startingConsonant;
        private static readonly Dictionary<string, string> startingConsonant = new Dictionary<string, string> {
            { "", "" },
            { "b", "b" },
            { "by", "by" },
            { "bw", "bw" },
            { "ch", "ch" },
            { "chy", "chy" },
            { "chw", "chw" },
            { "d", "d" },
            { "dy", "dy" },
            { "dw", "dw" },
            { "f", "f" },
            { "fy", "fy" },
            { "fw", "fw" },
            { "g", "g" },
            { "gy", "gy" },
            { "gw", "gw" },
            { "h", "h" },
            { "hy", "hy" },
            { "hw", "hw" },
            { "j", "j" },
            { "jy", "j" },
            { "jw", "jw" },
            { "k", "k" },
            { "ky", "ky" },
            { "kw", "kw" },
            { "l", "r" },
            { "ly", "ry" },
            { "lw", "rw" },
            { "m", "m" },
            { "my", "my" },
            { "mw", "mw" },
            { "n", "n" },
            { "ny", "ny" },
            { "nw", "nw" },
            { "p", "p" },
            { "py", "py" },
            { "pw", "pw" },
            { "r", "r" },
            { "ry", "ry" },
            { "rw", "rw" },
            { "rr", "rr" },
            { "rry", "rry" },
            { "rrw", "rrw" },
            { "s", "s" },
            { "sy", "sy" },
            { "sw", "sw" },
            { "t", "t" },
            { "ty", "ty" },
            { "tw", "tw" },
            { "w", "w" },
            { "y", "y" },
            { "z", "s" },
            { "zy", "sy" },
            { "zw", "sw" },
        };

        private Dictionary<string, string> SoloConsonant => soloConsonant;
        private static readonly Dictionary<string, string> soloConsonant = new Dictionary<string, string> {
            { "b", "ぶ" },
            { "by", "び" },
            { "bw", "ぶ" },
            { "ch", "ち" },
            { "chy", "ち" },
            { "chw", "ちゅ" },
            { "d", "ど" },
            { "dy", "でぃ" },
            { "dw", "どぅ" },
            { "f", "ふ" },
            { "fy", "ふぃ" },
            { "fw", "ふ" },
            { "g", "ぐ" },
            { "gy", "ぎ" },
            { "gw", "ぐ" },
            { "h", "ほ" },
            { "hy", "ひ" },
            { "hw", "ふ" },
            { "j", "じゅ" },
            { "jy", "じ" },
            { "jw", "じゅ" },
            { "k", "っ" },
            { "ky", "き" },
            { "kw", "く" },
            { "m", "む" },
            { "my", "み" },
            { "mw", "む" },
            { "n", "ん" },
            { "ny", "に" },
            { "nw", "ぬ" },
            { "p", "っ" },
            { "py", "ぴ" },
            { "pw", "ぷ" },
            { "r", "る" },
            { "ry", "り" },
            { "rw", "る" },
            { "s", "す" },
            { "sy", "し" },
            { "sh", "し" },
            { "sw", "す" },
            { "t", "っ" },
            { "ty", "てぃ" },
            { "tw", "とぅ" },
            { "ts", "つ" },
            { "tsy", "ち" },
            { "tsw", "つ" },
            { "w", "う" },
            { "y", "い" },
            { "z", "ず" },
            { "zy", "じ" },
            { "zw", "ず" },
            { "v", "ヴ" },
            { "vy", "ヴぃ" },
            { "vw", "ヴ" }
        };

        private Dictionary<string, string> AltCv => altCv;
        private static readonly Dictionary<string, string> altCv = new Dictionary<string, string> {
            {"kui", "ki" },
            {"sui", "suli" },
            {"tui", "teli" },
            {"nui", "ni" },
            {"hui", "hi" },
            {"mui", "mi" },
            {"rui", "ri" },
            {"gui", "gi" },
            {"zui", "zuli" },
            {"dui", "deli" },
            {"bui", "bi" },
            {"pui", "pi" },
            {"fui", "fi" },
            {"vui", "vi" },
            {"thui", "thi" },
            {"rrui", "rri" },
            {"lui", "li" },
            {"kwa", "kula" },
            {"kwi", "kuli" },
            {"kwe", "kule" },
            {"kwo", "kulo" },
            {"gwa", "gula" },
            {"gwi", "guli" },
            {"gwe", "gule" },
            {"gwo", "gulo" },
            {"swa", "sula" },
            {"swi", "suli" },
            {"swe", "sule" },
            {"swo", "sulo" },
            {"zwa", "zula" },
            {"zwi", "zuli" },
            {"zwe", "zule" },
            {"zwo", "zulo" },
            {"tswa", "tsula" },
            {"tswi", "tsuli" },
            {"tswe", "tsule" },
            {"tswo", "tsulo" },
            {"tsi", "chi" },
            {"tsye", "che" },
            {"tsya", "cha" },
            {"tsyu", "chu" },
            {"tsyo", "cho" },
            {"ti", "teli" },
            {"tya", "telya" },
            {"tyu", "telyu" },
            {"tye", "tele" },
            {"tyo", "telyo" },
            {"tu", "tolu" },
            {"di", "deli" },
            {"dya", "delya" },
            {"dyu", "delyu" },
            {"dye", "dele" },
            {"dyo", "delyo" },
            {"du", "dolu" },
            {"nwa", "nula" },
            {"nwi", "nuli" },
            {"nwe", "nule" },
            {"nwo", "nulo" },
            {"bwa", "bula" },
            {"bwi", "buli" },
            {"bwe", "bule" },
            {"bwo", "bulo" },
            {"pwa", "pula" },
            {"pwi", "puli" },
            {"pwe", "pule" },
            {"pwo", "pulo" },
            {"mwa", "mula" },
            {"mwi", "muli" },
            {"mwe", "mule" },
            {"mwo", "mulo" },
            {"rwa", "rula" },
            {"rwi", "ruli" },
            {"rwe", "rule" },
            {"rwo", "rulo" },
            {"wi", "uli" },
            {"we", "ule" },
            {"wo", "ulo" },
        };

        private Dictionary<string, string> ConditionalAlt => conditionalAlt;
        private static readonly Dictionary<string, string> conditionalAlt = new Dictionary<string, string> {
            {"ui", "uli" },
            {"uli", "wi" },
            {"ule", "we" },
            {"ulo", "wo"},
            {"kye", "ke" },
            {"kula", "ka" },
            {"kuli", "ki" },
            {"kule", "ke"  },
            {"kulo", "ko"  },
            {"gye", "ge" },
            {"gula", "ga" },
            {"guli", "gi" },
            {"gule", "ge" },
            {"gulo", "go" },
            {"sye", "se" },
            {"sula", "sa" },
            {"suli", "shi" },
            {"sule", "se" },
            {"sulo", "so" },
            {"zye", "ze" },
            {"zula", "za" },
            {"zuli", "zi" },
            {"zule", "ze" },
            {"zulo", "zo" },
            {"teli", "ti" },
            {"telya", "tya" },
            {"telyu", "tyu" },
            {"tele", "te" },
            {"telyo", "tyo" },
            {"tolu", "tsu" },
            {"tsye", "che" },
            {"tsula", "cha" },
            {"tsuli", "chi" },
            {"tsule", "tse" },
            {"tsulo", "cho" },
            {"deli", "ji" },
            {"delya", "ja" },
            {"delyu", "ju" },
            {"dele", "de" },
            {"delyo", "jo" },
            {"dolu", "zu" },
            {"nye", "ne" },
            {"nula", "na" },
            {"nuli", "ni" },
            {"nule", "ne" },
            {"nulo", "no" },
            {"hye", "he" },
            {"fa", "ha" },
            {"fi", "hi" },
            {"fe", "he" },
            {"fo", "ho" },
            {"bye", "be" },
            {"bula", "ba" },
            {"buli", "bi" },
            {"bule", "be" },
            {"bulo", "bo" },
            {"pye", "pe" },
            {"pula", "pa" },
            {"puli", "pi"},
            {"pule", "pe"},
            {"pulo", "po" },
            {"mye", "me" },
            {"mula", "ma" },
            {"muli", "mi" },
            {"mule", "me" },
            {"mulo", "mo" },
            {"ye", "e" },
            {"rye", "re" },
            {"rula", "ra" },
            {"ruli", "ri" },
            {"rule", "re" },
            {"rulo", "ro" },
            {"k", "ltsu" },
            {"t", "ltsu" },
            {"r", "ru" },
            {"m", "mu" },
            {"p", "ltsu" },
        };

        public override Result Process(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour, Note[] prevNeighbours) {
            Note note = notes[0];
            string color = string.Empty;
            int shift = 0;

            PhonemeAttributes attr = note.phonemeAttributes.FirstOrDefault(a => a.index == 0);
            color = attr.voiceColor;
            shift = attr.toneShift;

            string[] currIMF;
            string currPhoneme;
            string[] prevIMF;

            // Check if lyric is R, - or an end breath and return appropriate Result; otherwise, move to next steps
            if (note.lyric == "R" || note.lyric == "-" || note.lyric == "息" || note.lyric == "吸") {
                currPhoneme = note.lyric;

                if (prevNeighbour == null) {
                    return new Result {
                        phonemes = new Phoneme[] {
                            new Phoneme { phoneme = currPhoneme }
                        }
                    };
                } else {
                    if (singer.TryGetMappedOto(prevNeighbour?.lyric, note.tone + shift, color, out _)) {
                        string lastSound = GetLastSoundOfAlias(prevNeighbour?.lyric);
                        currPhoneme = $"{lastSound} {currPhoneme}";
                        //var tests = new List<string> { $"{lastSound} {currPhoneme}", currPhoneme };
                        //if (checkOtoUntilHit(tests, note, out var oto)) {
                        //    currPhoneme = oto.Alias;
                        //}

                        //if (singer.TryGetMappedOto($"{lastSound} {currPhoneme}", note.tone + shift, color, out _)) {
                        //    currPhoneme = $"{lastSound} {currPhoneme}";
                        //} else {
                        //    currPhoneme = currPhoneme;
                        //} 
                    } else {
                        if (string.IsNullOrEmpty(prevNeighbour?.phoneticHint)) {
                            byte[] bytes = Encoding.Unicode.GetBytes($"{prevNeighbour?.lyric[0]}");
                            int numval = Convert.ToInt32(bytes[0]) + Convert.ToInt32(bytes[1]) * (16 * 16);
                            if (prevNeighbour?.lyric.Length == 1 && numval >= 44032 && numval <= 55215) prevIMF = GetIMF(prevNeighbour?.lyric);
                            else return new Result {
                                phonemes = new Phoneme[] {
                                    new Phoneme { phoneme = currPhoneme }
                                }
                            };
                        } else prevIMF = GetIMFFromHint(prevNeighbour?.phoneticHint);

                        //var tests = new List<string> { $"{prevIMF[2]} {currPhoneme}", currPhoneme };
                        //if (checkOtoUntilHit(tests, note, out var oto)) {
                        //    currPhoneme = oto.Alias;
                        //}

                        if (string.IsNullOrEmpty(prevIMF[2])) currPhoneme = $"{((prevIMF[1][0] == 'w' || prevIMF[1][0] == 'y') ? prevIMF[1].Remove(0, 1) : ((prevIMF[1] == "ui") ? "i" : prevIMF[1]))} {currPhoneme}";
                        else currPhoneme = $"{(!singer.TryGetMappedOto($"{prevIMF[2]} {currPhoneme}", note.tone + shift, color, out _) ? null : prevIMF[2])} {currPhoneme}";
                        //else {
                        //currPhoneme = note.lyric;
                        //} else {
                        //    currPhoneme = note.lyric;
                        //}    
                    }

                    return new Result {
                        phonemes = new Phoneme[] {
                            new Phoneme { phoneme = currPhoneme }
                        }
                    };
                }
            }

            // Get IMF of current note if valid, otherwise return the lyric as is
            if (string.IsNullOrEmpty(note.phoneticHint)) {
                byte[] bytes = Encoding.Unicode.GetBytes($"{note.lyric[0]}");
                int numval = Convert.ToInt32(bytes[0]) + Convert.ToInt32(bytes[1]) * (16 * 16);
                if (note.lyric.Length == 1 && numval >= 44032 && numval <= 55215) currIMF = GetIMF(note.lyric);
                else return new Result {
                    phonemes = new Phoneme[] {
                        new Phoneme { phoneme = note.lyric }
                    }
                };
            } else currIMF = GetIMFFromHint(note.phoneticHint);

            // Convert current note to phoneme
            currPhoneme = $"{currIMF[0]}{currIMF[1]}";
            //var symbol = currIMF[0];
            //var solo = SoloConsonant[currIMF[0]];
            //currIMF[0] == ;

            currPhoneme = AltCv.ContainsKey(currPhoneme) ? AltCv[currPhoneme] : currPhoneme;

            if (singer.TryGetMappedOto(ToHiragana(currPhoneme), note.tone + shift, color, out _)) {
                currPhoneme = ToHiragana(currPhoneme);
            } else if (ConditionalAlt.ContainsKey(currPhoneme) && !singer.TryGetMappedOto(ToHiragana(currPhoneme), note.tone + shift, color, out _)) {
                currPhoneme = ConditionalAlt[currPhoneme];
            }
            // Adjust current phoneme based on previous neighbor
            if (prevNeighbour != null && singer.TryGetMappedOto(prevNeighbour?.lyric, note.tone + shift, color, out _) && !singer.TryGetMappedOto(prevNeighbour?.lyric, note.tone + shift, color, out _)) {

                var tests = new List<string> { prevNeighbour?.lyric, ToHiragana(currPhoneme) };
                if (checkOtoUntilHit(tests, note, out var oto)) {
                    currPhoneme = ToHiragana(currPhoneme);
                }

                //currPhoneme = $"{GetLastSoundOfAlias(prevNeighbour?.lyric)} {ToHiragana(currPhoneme)}";
                //} else if (!singer.TryGetMappedOto(prevNeighbour?.lyric, note.tone + shift, color, out _)) {
                //    currPhoneme = ToHiragana(currPhoneme);
                //}
                //} else if (prevNeighbour != null && !singer.TryGetMappedOto(prevNeighbour?.lyric, note.tone + shift, color, out _) && singer.TryGetMappedOto(ToHiragana(currPhoneme), note.tone + shift, color, out _)) {
                //    currPhoneme = ToHiragana(currPhoneme);


            } else {
                if (prevNeighbour == null || prevNeighbour?.lyric == "R" || prevNeighbour?.lyric == "-" || prevNeighbour?.lyric == "息" || prevNeighbour?.lyric == "吸") {
                    var tests = new List<string> { $"- {ToHiragana(currPhoneme)}", ToHiragana(currPhoneme) };
                    if (checkOtoUntilHit(tests, note, out var oto)) {
                        currPhoneme = oto.Alias;
                    }
                } else {
                    if (string.IsNullOrEmpty(prevNeighbour?.phoneticHint)) {
                        byte[] bytes = Encoding.Unicode.GetBytes($"{prevNeighbour?.lyric[0]}");
                        int numval = Convert.ToInt32(bytes[0]) + Convert.ToInt32(bytes[1]) * (16 * 16);
                        if (prevNeighbour?.lyric.Length == 1 && numval >= 44032 && numval <= 55215) prevIMF = GetIMF(prevNeighbour?.lyric);
                        else return new Result {
                            phonemes = new Phoneme[] {
                                new Phoneme { phoneme = note.lyric }
                            }
                        };
                    } else prevIMF = GetIMFFromHint(prevNeighbour?.phoneticHint);

                    string prevConnect;

                    //var symbol = currIMF[0];
                    //var solo2 = SoloConsonant[prevIMF[2]];

                    if (!string.IsNullOrEmpty(prevIMF[2])) {
                        if (Array.IndexOf(sonorants, prevIMF[2]) > -1) {
                            prevConnect = prevIMF[2];
                            var tests = new List<string> { prevIMF[2], null };
                            if (checkOtoUntilHit(tests, note, out var oto)) {
                                prevConnect = prevIMF[2];
                            }
                            //if (singer.TryGetMappedOto($"{prevIMF[2]} {ToHiragana(currPhoneme)}", note.tone + shift, color, out _)) prevConnect = prevIMF[2];
                            //else 
                            //var tests = new List<string> { ToHiragana(currPhoneme) };
                            //if (checkOtoUntilHit(tests, note, out var oto)) {
                            //    currPhoneme = oto.Alias;
                            //    //prevConnect = prevIMF[2];
                            //    //} else {
                            //    //prevConnect = null;
                            //};


                            //if (singer.TryGetMappedOto($"{prevIMF[2]} {ToHiragana(currPhoneme)}", note.tone + shift, color, out _)) prevConnect = prevIMF[2];
                            //else if (singer.TryGetMappedOto($"{prevIMF[2]} {ToHiragana(ConditionalAlt[currPhoneme])}", note.tone + shift, color, out _)) prevConnect = prevIMF[2];
                            // else prevConnect = null;
                        } else if (Array.IndexOf(otherSonorants, prevIMF[2]) > -1) {
                            prevConnect = "u";
                            var tests = new List<string> { "u", null };
                            if (checkOtoUntilHit(tests, note, out var oto)) {
                                prevConnect = "u";
                            }
                            //var tests = new List<string> { ToHiragana(currPhoneme) };
                            //if (checkOtoUntilHit(tests, note, out var oto)) {
                            //    currPhoneme = oto.Alias;
                            //    //prevConnect = prevIMF[2];
                            //    //} else {
                            //    //    prevConnect = null;
                            //}

                            //if (prevIMF[2] == "m" && currIMF[0] == "m" || currIMF[0] == "p" || currIMF[0] == "b") {
                            //    prevConnect = "n";
                            //    prevIMF[2] = "n";
                            //};

                            //if (prevIMF[2] == "r" && currIMF[0] == "r") {
                            //    prevConnect = GetLastSoundOfAlias(prevNeighbour?.lyric);
                            //prevIMF[2] = null;
                            //var tests2 = new List<string> { ToHiragana(currPhoneme) };

                            //};

                            //prevConnect = null;
                            //var tests = new List<string> { prevIMF[2], $"{prevIMF[2]}u" };
                            //if (checkOtoUntilHit(tests, note, out var oto)) {
                            //     prevConnect = prevIMF[2];
                            //} else {
                            //    prevConnect = null;
                            //}

                            //if (singer.TryGetMappedOto($"{prevIMF[2]} {ToHiragana(currPhoneme)}", note.tone + shift, color, out _)) prevConnect = prevIMF[2];
                            //else if (singer.TryGetMappedOto($"{prevIMF[2]} {ToHiragana(ConditionalAlt[currPhoneme])}", note.tone + shift, color, out _)) prevConnect = prevIMF[2];
                            //else if (singer.TryGetMappedOto($"{prevIMF[2]}u {ToHiragana(currPhoneme)}", note.tone + shift, color, out _)) prevConnect = "u";
                            //else if (singer.TryGetMappedOto($"{prevIMF[2]}u {ToHiragana(ConditionalAlt[currPhoneme])}", note.tone + shift, color, out _)) prevConnect = "u";
                            //else prevConnect = null;
                        } else {
                            prevConnect = "-";
                            var tests = new List<string> { "-", null };
                            if (checkOtoUntilHit(tests, note, out var oto)) {
                                prevConnect = "-";
                            }
                                
                            //var tests = new List<string> { ToHiragana(currPhoneme) };
                            //if (checkOtoUntilHit(tests, note, out var oto)) {
                            //    //currPhoneme.Replace('ー', '-');
                            //    currPhoneme = oto.Alias;
                            //};
                            //if (!singer.TryGetMappedOto($"- {ToHiragana(currPhoneme)}", note.tone + shift, color, out _)) {
                            //    prevConnect = null;
                            //}

                            //    prevConnect = "-";
                            //} else {
                            //    prevConnect = null;
                            //}
                            //prevConnect = null;


                                //if (singer.TryGetMappedOto($"- {ToHiragana(currPhoneme)}", note.tone + shift, color, out _)) {
                                //prevConnect = "-";
                                //} else {
                                //prevConnect = null;
                                //}



                                //else if (singer.TryGetMappedOto($"- {ToHiragana(ConditionalAlt[currPhoneme])}", note.tone + shift, color, out _)) prevConnect = "-";
                                //else prevConnect = null;
                        };
                    } else {
                        if (prevIMF[1][0] == 'w' || prevIMF[1][0] == 'y') prevConnect = prevIMF[1].Remove(0, 1);
                        else if (prevIMF[1] == "ui") prevConnect = "i";
                        else
                        //if (prevIMF[1] == prevIMF[1]) prevConnect = prevIMF[1];
                        //else
                        prevConnect = prevIMF[1];
                    }

                    //currPhoneme = AltCv.ContainsKey(currPhoneme) ? AltCv[currPhoneme] : currPhoneme;

                    //if (singer.TryGetMappedOto($"{prevConnect} {ToHiragana(currPhoneme)}", note.tone + shift, color, out _)) {
                    //var tests2 = new List<string> { $"{prevConnect} {ToHiragana(currPhoneme)}", ToHiragana(currPhoneme) };
                    //if (checkOtoUntilHit(tests2, note, out var oto2)) {
                    currPhoneme = $"{prevConnect} {ToHiragana(currPhoneme)}";
                    //prevConnect = prevIMF[2];
                    //} else {
                    //prevConnect = null;
                    //};

                    //currPhoneme = $"{prevConnect} {ToHiragana(currPhoneme)}";
                    //if (prevConnect == null) {
                    //    currPhoneme = ToHiragana(currPhoneme).Trim(' ');
                    //}

                    //if ((prevNeighbour == null || prevNeighbour?.lyric == "-" || prevNeighbour?.lyric == "R" || prevNeighbour?.lyric == "息" || prevNeighbour?.lyric == "吸") && !singer.TryGetMappedOto($"- {ToHiragana(currPhoneme)}", note.tone + shift, color, out _) && singer.TryGetMappedOto($"- {ToHiragana(ConditionalAlt[currPhoneme])}", note.tone + shift, color, out _)) {
                    //    currPhoneme = $"- {ToHiragana(ConditionalAlt[currPhoneme])}";
                    //    if (!singer.TryGetMappedOto($"- {ToHiragana(ConditionalAlt[currPhoneme])}", note.tone + shift, color, out _) && singer.TryGetMappedOto(ToHiragana(ConditionalAlt[currPhoneme]), note.tone + shift, color, out _)) {
                    //        currPhoneme = ToHiragana(ConditionalAlt[currPhoneme]);
                    //    }
                    //} else if (!singer.TryGetMappedOto($"- {ToHiragana(ConditionalAlt[currPhoneme])}", note.tone + shift, color, out _) && singer.TryGetMappedOto(ToHiragana(ConditionalAlt[currPhoneme]), note.tone + shift, color, out _)) {
                    //    currPhoneme = ToHiragana(ConditionalAlt[currPhoneme]);
                    //} else {
                    //    currPhoneme = ToHiragana(currPhoneme);
                    //}
                    //} else if (AltCv.ContainsKey(currPhoneme) && singer.TryGetMappedOto($"{prevConnect} {AltCv[ToHiragana(currPhoneme)]}", note.tone + shift, color, out _)) {
                    //    currPhoneme = $"{prevConnect} {AltCv[ToHiragana(currPhoneme)]}";
                    //} else {
                    //    currPhoneme = ToHiragana(currPhoneme);
                    //}
                    //if (prevNeighbour != null && !singer.TryGetMappedOto($"{prevConnect} {ToHiragana(currPhoneme)}", note.tone + shift, color, out _)) {
                    //    //var vowel = "";
                    //    //if (prevIMF[1][0] == 'w' || prevIMF[1][0] == 'y') prevConnect = prevIMF[1].Remove(0, 1);
                    //    //else if (prevIMF[1] == "ui") prevConnect = "i";
                    //    //else
                    //    //    //if (prevIMF[1] == prevIMF[1]) prevConnect = prevIMF[1];
                    //    //    //else
                    //    //    prevConnect = prevIMF[1];
                    //    var vc = $"{prevConnect} {currIMF[1]}";

                    //    int totalDuration = notes.Sum(n => n.duration);
                    //    int vcLength = 120;
                    //    var nextAttr = nextNeighbour.Value.phonemeAttributes?.FirstOrDefault(attr => attr.index == 0) ?? default;
                    //    if (singer.TryGetMappedOto(currPhoneme, nextNeighbour.Value.tone + nextAttr.toneShift, nextAttr.voiceColor, out var oto)) {
                    //        // If overlap is a negative value, vcLength is longer than Preutter
                    //        if (oto.Overlap < 0) {
                    //            vcLength = MsToTick(oto.Preutter - oto.Overlap);
                    //        } else {
                    //            vcLength = MsToTick(oto.Preutter);
                    //        }
                    //    }
                    //    // vcLength depends on the Vel of the next note
                    //    vcLength = Convert.ToInt32(Math.Min(totalDuration / 2, vcLength * (nextAttr.consonantStretchRatio ?? 1)));

                    //    if (singer.TryGetMappedOto(vc, note.tone + shift, color, out _) && singer.TryGetMappedOto(currPhoneme, note.tone + shift, color, out _)) {
                    //        return new Result {
                    //            phonemes = new Phoneme[] {
                    //            new Phoneme() {
                    //            phoneme = currPhoneme,
                    //        },
                    //            new Phoneme() {
                    //            phoneme = vc,
                    //            position = totalDuration - vcLength,
                    //            }
                    //        },
                    //        };
                    //    }

                    //    //if (plainVowels.TryGetValue(originalCurrentLyric.LastOrDefault().ToString() ?? string.Empty, out var vow)) {
                    //    //    vowel = vow;
                    //    //}
                    //}
                }


            }

            // Return Result now if note has no batchim
            if (string.IsNullOrEmpty(currIMF[2])) {
                return new Result {
                    phonemes = new Phoneme[] {
                        new Phoneme { phoneme = currPhoneme }
                    }
                };
            }

            int totalDuration = notes.Sum(n => n.duration);
            if (prevNeighbour != null && singer.TryGetMappedOto($"{GetLastSoundOfAlias(prevNeighbour?.lyric)} {currIMF[0]}", note.tone + shift, color, out var oto3)) {
                return MakeSimpleResult(oto3.Alias);
            }
            int vcLen = 120;
            if (singer.TryGetMappedOto(ToHiragana(currPhoneme), note.tone + shift, color, out var cvOto)) {
                vcLen = MsToTick(cvOto.Preutter);
                if (cvOto.Overlap == 0 && vcLen < 120) {
                    vcLen = Math.Min(120, vcLen * 2); // explosive consonant with short preutter.
                }
            }
            if (singer.TryGetMappedOto($"{GetLastSoundOfAlias(prevNeighbour?.lyric)} {currIMF[0]}", note.tone + shift, color, out oto3)) {
                return new Result {
                    phonemes = new Phoneme[] {
                        new Phoneme() {
                            phoneme = oto3.Alias,
                            position = -vcLen,
                        },
                        new Phoneme() {
                            phoneme = ToHiragana(currPhoneme),
                        },
                    },
                };
            }

        // Adjust Result if note has batchim
        else {
                string secondPhoneme = (currIMF[1][0] == 'w' || currIMF[1][0] == 'y' || currIMF[1] == "ui") ? currIMF[1].Remove(0, 1) : currIMF[1];
                if (nextNeighbour == null) {
                    if (string.IsNullOrEmpty(currIMF[2])) {
                        if (singer.TryGetMappedOto($"{secondPhoneme} R", note.tone + shift, color, out var oto)) {
                            secondPhoneme += " R";
                        } else {
                            secondPhoneme = "R";
                        }
                            
                    }
                //var tests = new List<string> { (secondPhoneme += $" R") };
                //if (checkOtoUntilHit(tests, note, out var oto)) {
                //    secondPhoneme = oto.Alias;
                //}
                //secondPhoneme += " R";
                //}
                else {
                        //var tests = new List<string> { (secondPhoneme += $" R"), "R" };
                        //if (checkOtoUntilHit(tests, note, out var oto)) {
                        //    secondPhoneme = oto.Alias;
                        //}

                        //secondPhoneme += " R";
                        //} else {
                        //var tests = new List<string> { $"{secondPhoneme} {currIMF[2]}", ToHiragana($"{secondPhoneme} {currIMF[2]}"), (secondPhoneme = currIMF[2]), ToHiragana(secondPhoneme = currIMF[2]) };
                        //if (checkOtoUntilHit(tests, note, out var oto)) {
                        //    secondPhoneme = oto.Alias;
                        //}
                        //if (singer.TryGetMappedOto(secondPhoneme += $" {currIMF[2]}", note.tone + shift, color, out var oto)) {
                        //    secondPhoneme += oto.Alias;
                        //} else if (singer.TryGetMappedOto(ToHiragana(secondPhoneme += $" {currIMF[2]}"), note.tone + shift, color, out var oto1)) {
                        //    secondPhoneme += oto1.Alias;
                        //} else if (singer.TryGetMappedOto(secondPhoneme = currIMF[2], note.tone + shift, color, out var oto2)) {
                        //    secondPhoneme += oto2.Alias;
                        //} else if (singer.TryGetMappedOto(ToHiragana(secondPhoneme = currIMF[2]), note.tone + shift, color, out var oto3)) {
                        //    secondPhoneme += oto3.Alias;
                        //}
                        //var tests = new List<string> { ToHiragana($" {currIMF[2]}"), $" {currIMF[2]}" };
                        //if (checkOtoUntilHit(tests, note, out var oto)) {
                        if (singer.TryGetMappedOto($"{secondPhoneme} {currIMF[2]}", note.tone + shift, color, out var oto)) {
                            secondPhoneme += $" {currIMF[2]}";
                        } else if (singer.TryGetMappedOto($"{secondPhoneme} {ToHiragana(currIMF[2])}", note.tone + shift, color, out var oto1)) {
                            secondPhoneme += $" {ToHiragana(currIMF[2])}";
                        } else if (singer.TryGetMappedOto($"{ToHiragana(currIMF[2])}", note.tone + shift, color, out var oto2)) {
                            secondPhoneme = ToHiragana(currIMF[2]);
                        } else {
                            secondPhoneme = currIMF[2];
                        }

                        //if (singer.TryGetMappedOto($"* {ToHiragana(currIMF[2])}", note.tone + shift, color, out var oto2)) {
                        //    //secondPhoneme += $"* {ToHiragana(currIMF[2])}";
                        //    //secondPhoneme.Remove();
                        //}
                        //}

                        //if (singer.TryGetMappedOto($"{secondPhoneme} {currIMF[2]}", note.tone + shift, color, out _)) secondPhoneme += $" {currIMF[2]}";
                        //else if (singer.TryGetMappedOto(currIMF[2], note.tone + shift, color, out _)) secondPhoneme += currIMF[2];
                        //else secondPhoneme += $" {ToHiragana(currIMF[2])}";
                    }
                } else if (!string.IsNullOrEmpty(currIMF[2])) {
                    //var tests = new List<string> { $"{secondPhoneme} {currIMF[2]}", ToHiragana($"{secondPhoneme} {currIMF[2]}"), (secondPhoneme = currIMF[2]), ToHiragana(secondPhoneme = currIMF[2]) };
                    //if (checkOtoUntilHit(tests, note, out var oto)) {
                    //    secondPhoneme = oto.Alias;
                    //}
                    //secondPhoneme += currIMF[2];

                    //if (currIMF[2] == null || currIMF[2] == "r" && secondPhoneme == "r") {
                    //    secondPhoneme = GetLastSoundOfAlias(prevNeighbour?.lyric);
                    //}
                    //if (singer.TryGetMappedOto(secondPhoneme += $" {currIMF[2]}", note.tone + shift, color, out var oto)) {
                    //    secondPhoneme += oto.Alias;
                    //} else if (singer.TryGetMappedOto(ToHiragana(secondPhoneme += $" {currIMF[2]}"), note.tone + shift, color, out var oto1)) {
                    //    secondPhoneme += oto1.Alias;
                    //} else if (singer.TryGetMappedOto(secondPhoneme = currIMF[2], note.tone + shift, color, out var oto2)) {
                    //    secondPhoneme += oto2.Alias;
                    //} else if (singer.TryGetMappedOto(ToHiragana(secondPhoneme = currIMF[2]), note.tone + shift, color, out var oto3)) {
                    //    secondPhoneme += oto3.Alias;
                    //}
                    //var tests = new List<string> { ToHiragana($" {currIMF[2]}"), $" {currIMF[2]}" };
                    //if (checkOtoUntilHit(tests, note, out var oto)) {
                    //secondPhoneme += $" {currIMF[2]}";
                    //}

                    if (singer.TryGetMappedOto($"{secondPhoneme} {currIMF[2]}", note.tone + shift, color, out var oto)) {
                        secondPhoneme += $" {currIMF[2]}";
                    } else if (singer.TryGetMappedOto($"{secondPhoneme} {ToHiragana(currIMF[2])}", note.tone + shift, color, out var oto1)) {
                        secondPhoneme += $" {ToHiragana(currIMF[2])}";
                    } else if (singer.TryGetMappedOto($"{ToHiragana(currIMF[2])}", note.tone + shift, color, out var oto2)) {
                        secondPhoneme = ToHiragana(currIMF[2]);
                    } else {
                        secondPhoneme = currIMF[2];
                    }

                    //if (singer.TryGetMappedOto($"* {ToHiragana(currIMF[2])}", note.tone + shift, color, out var oto2)) {
                    //    //ToHiragana(currIMF[2])}";
                    //    //secondPhoneme.Remove();
                    //}

                    //if (singer.TryGetMappedOto($"{secondPhoneme} {currIMF[2]}", note.tone + shift, color, out _)) secondPhoneme += $" {currIMF[2]}";
                    //else if (singer.TryGetMappedOto(currIMF[2], note.tone + shift, color, out _)) secondPhoneme += currIMF[2];
                    //else secondPhoneme += $" {ToHiragana(currIMF[2])}";
                }

                int noteLength = 0;
                for (int i = 0; i < notes.Length; i++) noteLength += notes[i].duration;

                int secondPosition = Math.Max(noteLength - (nextNeighbour == null ? 120 : 180), noteLength / 2);

                // Return Result
                return new Result {
                    phonemes = new Phoneme[] {
                        new Phoneme { phoneme = currPhoneme },
                        new Phoneme { phoneme = secondPhoneme, position = secondPosition }
                    }
                };
            }
        }
        private string ToHiragana(string romaji) {
            var hiragana = WanaKana.ToHiragana(romaji);
            hiragana = hiragana.Replace("ゔ", "ヴ");
            hiragana = hiragana.Replace("hわ", "ふぁ");
            hiragana = hiragana.Replace("hうぃ", "ふぃ");
            hiragana = hiragana.Replace("hうぇ", "ふぇ");
            hiragana = hiragana.Replace("hうぉ", "ふぉ");
            hiragana = hiragana.Replace("k", "く");
            hiragana = hiragana.Replace("t", "と");
            hiragana = hiragana.Replace("p", "ぷ");
            hiragana = hiragana.Replace("m", "む");
            hiragana = hiragana.Replace("r", "る");
            //hiragana = hiragana.Replace("bわ", "ぶぁ");
            //hiragana = hiragana.Replace("bうぃ", "ぶぃ");
            //hiragana = hiragana.Replace("bうぇ", "ぶぇ");
            //hiragana = hiragana.Replace("bうぉ", "ぶぉ");
            //hiragana = hiragana.Replace("pわ", "ぷぁ");
            //hiragana = hiragana.Replace("pうぃ", "ぷぃ");
            //hiragana = hiragana.Replace("pうぇ", "ぷぇ");
            //hiragana = hiragana.Replace("pうぉ", "ぷぉ");
            return hiragana;
        }

        // make it quicker to check multiple oto occurrences at once rather than spamming if else if
        private bool checkOtoUntilHit(List<string> input, Note note, out UOto oto) {
            oto = default;
            var attr = note.phonemeAttributes?.FirstOrDefault(attr => attr.index == 0) ?? default;

            var otos = new List<UOto>();
            foreach (string test in input) {
                if (singer.TryGetMappedOto(test, note.tone + attr.toneShift, attr.voiceColor, out var otoCandidacy)) {
                    otos.Add(otoCandidacy);
                }
            }

            string color = attr.voiceColor ?? "";
            if (otos.Count > 0) {
                if (otos.Any(oto => (oto.Color ?? string.Empty) == color)) {
                    oto = otos.Find(oto => (oto.Color ?? string.Empty) == color);
                    return true;
                } else {
                    return false;
                }
            }
            return false;
        }
    }
}
