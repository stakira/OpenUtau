using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using OpenUtau.Api;
using OpenUtau.Core.G2p;

namespace OpenUtau.Core.DiffSinger {
    [Phonemizer("DiffSinger English+ Phonemizer", "DIFFS EN+", language: "EN", author: "Cadlaxa")]
    public class DiffSingerARPAPlusEnglishPhonemizer : DiffSingerG2pPhonemizer
    // cadlaxa here, this diffsinger english phonemizer just uses the ARPA+ G2p so arpasing+ and this phonemizer
    // have same g2p mechanics such as triggering of glottal stop with ('), manual relaxed consonants
    // plus other ds features
    {
        protected override string GetDictionaryName() => "dsdict-en.yaml";
        protected override string GetLangCode() => "en";
        protected override IG2p LoadBaseG2p() => new ArpabetPlusG2p();
        protected override string[] GetBaseG2pVowels() => new string[] {
            "aa", "ae", "ah", "ao", "aw", "ax", "ay", "eh", "er",
            "ey","ih", "iy", "ow", "oy","uh", "uw"
        };
        protected override string[] GetBaseG2pConsonants() => new string[] {
            "b", "ch", "d", "dh", "dr", "dx", "f", "g", "hh", "jh",
            "k", "l", "m", "n", "ng", "p", "q", "r", "s", "sh", "t",
            "th", "tr", "v", "w", "y", "z", "zh"
        };
        public override Result Process(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour, Note[] prevs) {

            if (notes[0].lyric == "-") {
                return MakeSimpleResult("SP");
            }
            if (notes[0].lyric == "br") {
                return MakeSimpleResult("AP");
            }
            if (!partResult.TryGetValue(notes[0].position, out var phonemes)) {
                throw new Exception("Result not found in the part");
            }
            var processedPhonemes = new List<Phoneme>();

            for (int i = 0; i < phonemes.Count; i++) {
                var tu = phonemes[i];

                // Check for "n dx" sequence and replace it with "n"
                // the actual phoneme for this is "nx" like (winner [w ih nx er])
                if (i < phonemes.Count - 1 && tu.Item1 == "n" && phonemes[i + 1].Item1 == "dx") {
                    processedPhonemes.Add(new Phoneme() {
                        phoneme = "n",
                        position = tu.Item2
                    });
                    // Skip the next phoneme ("dx")
                    i++;
                } else if (ShouldReplacePhoneme(tu.Item1, prev, next, prevNeighbour, nextNeighbour, out string replacement)) {
                    processedPhonemes.Add(new Phoneme() {
                        phoneme = replacement,
                        position = tu.Item2
                    });
                } else {
                    processedPhonemes.Add(new Phoneme() {
                        phoneme = tu.Item1,
                        position = tu.Item2
                    });
                }
            }
            return new Result {
                phonemes = processedPhonemes.ToArray()
            };
        }

        // Method to determine if a phoneme should be replaced based on specific conditions
        private bool ShouldReplacePhoneme(string phoneme, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour, out string replacement) {
            replacement = phoneme;
            if (phoneme == "q") {
                replacement = "cl";
                return true;
            }
            if (phoneme == "q") {
                // vocal fry the vowel is the prevNeighbour is null
                if (!prevNeighbour.HasValue || string.IsNullOrWhiteSpace(prevNeighbour.Value.lyric)) {
                replacement = "vf";
                return true;
                }
            }
            // automatic relaxed consonants
            if ((phoneme == "t" || phoneme == "d") && (nextNeighbour.HasValue && IsVowel(nextNeighbour.Value))) {
                replacement = "dx";
                return true;
            }
            return false;
        }
        // Method to check if a phoneme is a vowel
        private bool IsVowel(Note note) {
            string[] vowels = GetBaseG2pVowels();
            return vowels.Contains(note.lyric);
        }
    }
}
