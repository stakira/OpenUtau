using System;
using System.Linq;
using System.Text;
using OpenUtau.Api;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Plugin.Builtin
{
	[Phonemizer("Korean VCV Phonemizer", "KO VCV", "ldc")]

	public class KoreanVCVPhonemizer : Phonemizer {
		/// <summary>
		/// Initial jamo as ordered in Unicode
		/// </summary>
		static readonly string[] initials = { "g", "gg", "n", "d", "dd", "r", "m", "b", "bb", "s", "ss", string.Empty, "j", "jj", "ch", "k", "t", "p", "h" };
		// ㄱ ㄲ ㄴ ㄷ ㄸ ㄹ ㅁ ㅂ ㅃ ㅅ ㅆ ㅇ ㅈ ㅉ ㅊ ㅋ ㅌ ㅍ ㅎ

        /// <summary>
        /// Medial jamo as ordered in Unicode
        /// </summary>
		static readonly string[] medials = { "a", "e", "ya", "ye", "eo", "e", "yeo", "ye", "o", "wa", "we", "oe", "yo", "u", "weo", "we", "wi", "yu", "eu", "ui", "i" };
		// ㅏ ㅐ ㅑ ㅒ ㅓ ㅔ ㅕ ㅖ ㅗ ㅘ ㅙ ㅚ ㅛ ㅜ ㅝ ㅞ ㅟ ㅠ ㅡ ㅢ ㅣ

        /// <summary>
        /// Final jamo as ordered in Unicode
        /// </summary>
		static readonly string[] finals = { string.Empty, "k", "k", "k", "n", "n", "n", "t", "l", "l", "l", "l", "l", "l", "l", "l", "m", "p", "p", "t", "t", "ng", "t", "t", "k", "t", "p", "t" };
		// - ㄱ ㄲ ㄳ ㄴ ㄵ ㄶ ㄷ ㄹ ㄺ ㄻ ㄼ ㄽ ㄾ ㄿ ㅀ ㅁ ㅂ ㅄ ㅅ ㅆ ㅇ ㅈ ㅊ ㅋ ㅌ ㅍ ㅎ
		
        /// <summary>
        /// Sonorant batchim (i.e., extendable batchim sounds)
        /// </summary>
		static readonly string[] sonorants = { "n", "m", "ng", "l" };

        /// <summary>
        /// Extra English-based sounds for phonetic hint input
        /// </summary>
        static readonly string[] extras = { "f", "v", "th", "dh", "z", "l" };

		/// <summary>
		/// Gets the romanized initial, medial, and final components of the passed Hangul syllable.
		/// </summary>
		/// <param name="syllable">A Hangul syllable.</param>
		/// <returns>An array containing the initial, medial, and final sounds of the syllable.</returns>
		public string[] GetIMF(string syllable) {
			byte[] bytes = Encoding.Unicode.GetBytes(syllable);
			int numval = Convert.ToInt16(bytes[0]) + Convert.ToInt16(bytes[1]) * (16 * 16);
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
        public string [] GetIMFFromHint(string hint) {
            string[] hintSplit = hint.Split(' ');

            string i = Array.IndexOf(initials.Concat(extras).ToArray(), hintSplit[0]) > -1 ? hintSplit[0] : string.Empty;
            string m = string.IsNullOrEmpty(i) ? hintSplit[0] : hintSplit[1];
            string f = (hintSplit.Length > 2 || (hintSplit.Length == 2 && string.IsNullOrEmpty(i))) ? hintSplit[^1] : string.Empty;

            string[] ret = { i, m, f };

            return ret;
        }

		private USinger singer;

		// Store singer
		public override void SetSinger(USinger singer) => this.singer = singer;

		public override Result Process(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour, Note[] prevNeighbours) {
			var note = notes[0];

            // Check if lyric is R or - and return appropriate Result; otherwise, move to next steps
            if (note.lyric == "R" || note.lyric == "-") {
                if (prevNeighbour == null) {
                    return new Result {
                        phonemes = new Phoneme[] {
                            new Phoneme { phoneme = note.lyric }
                        }
                    };
                } else {
                    string RPhoneme = note.lyric;
                    string[] RprevIMF = (string.IsNullOrEmpty(prevNeighbour?.phoneticHint)) ? GetIMF(prevNeighbour?.lyric) : GetIMFFromHint(prevNeighbour?.phoneticHint);

                    if (string.IsNullOrEmpty(RprevIMF[2])) RPhoneme = $"{((RprevIMF[1][0] == 'w' || RprevIMF[1][0] == 'y' || RprevIMF[1] == "oe" || RprevIMF[1] == "ui") ? RprevIMF[1].Remove(0, 1) : RprevIMF[1])} {RPhoneme}";
                    else {
                        if (Array.IndexOf(sonorants, RprevIMF[2]) > -1) {
                            if (singer.TryGetMappedOto($"{RprevIMF[2]} {RPhoneme}", note.tone, out _)) RPhoneme = $"{RprevIMF[2]} {RPhoneme}";
                            else RPhoneme = $"{RprevIMF[2].ToUpper()} {RPhoneme}";
                        } else RPhoneme = "- " + RPhoneme;
                    }

                    return new Result {
                        phonemes = new Phoneme[] {
                            new Phoneme { phoneme = RPhoneme }
                        }
                    };
                }
            }

            // Get IMF of current note if valid, otherwise return the lyric as is
            string[] currIMF;

            if (string.IsNullOrEmpty(note.phoneticHint)) {
                byte[] bytes = Encoding.Unicode.GetBytes($"{note.lyric[0]}");
                int numval = Convert.ToInt16(bytes[0]) + Convert.ToInt16(bytes[1]) * (16 * 16);
                if (note.lyric.Length == 1 && numval >= 44032 && numval <= 55215) currIMF = GetIMF(note.lyric);
                else return new Result {
                    phonemes = new Phoneme[] {
                        new Phoneme { phoneme = note.lyric }
                    }
                };
            } else currIMF = GetIMFFromHint(note.phoneticHint);
			
			// Convert current note to phoneme
			string currPhoneme = $"{currIMF[0] + currIMF[1]}";

            // Adjust current phoneme based on previous neighbor
            string[] prevIMF;

			if (prevNeighbour == null || prevNeighbour?.lyric == "R" || prevNeighbour?.lyric == "-") currPhoneme = $"- {currPhoneme}";
			else {
                if (string.IsNullOrEmpty(prevNeighbour?.phoneticHint)) {
                    byte[] bytes = Encoding.Unicode.GetBytes($"{prevNeighbour?.lyric[0]}");
                    int numval = Convert.ToInt16(bytes[0]) + Convert.ToInt16(bytes[1]) * (16 * 16);
                    if (prevNeighbour?.lyric.Length == 1 && numval >= 44032 && numval <= 55215) prevIMF = GetIMF(prevNeighbour?.lyric);
                    else return new Result {
                        phonemes = new Phoneme[] {
                            new Phoneme { phoneme = note.lyric }
                        }
                    };
                } else prevIMF = GetIMFFromHint(prevNeighbour?.phoneticHint);

				if (string.IsNullOrEmpty(prevIMF[2])) currPhoneme = $"{((prevIMF[1][0] == 'w' || prevIMF[1][0] == 'y' || prevIMF[1] == "oe" || prevIMF[1] == "ui") ? prevIMF[1].Remove(0, 1) : prevIMF[1])} {currPhoneme}";
				else {
					if (Array.IndexOf(sonorants, prevIMF[2]) > -1) {
						if (singer.TryGetMappedOto($"{prevIMF[2]} {currPhoneme}", note.tone, out _)) currPhoneme = $"{prevIMF[2]} {currPhoneme}";
						else currPhoneme = $"{prevIMF[2].ToUpper()} {currPhoneme}";
					} else currPhoneme = "- " + currPhoneme;
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

            // Adjust Result if note has batchim
            else {
			    string secondPhoneme = (currIMF[1][0] == 'w' || currIMF[1][0] == 'y' || currIMF[1] == "oe" || currIMF[1] == "ui") ? currIMF[1].Remove(0, 1) : currIMF[1];

			    if (nextNeighbour == null) {
				    if (string.IsNullOrEmpty(currIMF[2])) secondPhoneme += " R";
				    else {
					    if (singer.TryGetMappedOto($"{secondPhoneme} {currIMF[2]}", note.tone, out _)) secondPhoneme += $" {currIMF[2]}";
					    else secondPhoneme += $" {currIMF[2].ToUpper()}";
				    }
			    } else if (!string.IsNullOrEmpty(currIMF[2])) {
				    if (singer.TryGetMappedOto($"{secondPhoneme} {currIMF[2]}", note.tone, out _)) secondPhoneme += $" {currIMF[2]}";
				    else secondPhoneme += $" {currIMF[2].ToUpper()}";
			    }

                int secondPosition = Math.Max(note.duration - (nextNeighbour == null ? 120 : 180), note.duration / 2);

                // Return Result
                return new Result {
                    phonemes = new Phoneme[] {
                        new Phoneme { phoneme = currPhoneme },
                        new Phoneme { phoneme = secondPhoneme, position = secondPosition }
                    }
                };
			}
		}
	}
}
