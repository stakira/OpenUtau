using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using OpenUtau.Core.G2p;

namespace OpenUtau.Core.Neutrino {
    // Generate only the score features consumed by the Tau models, without HTS labels.
    internal static class NeutrinoPhonemes {
        static readonly JapaneseMonophoneG2p dictionary = new JapaneseMonophoneG2p();
        internal static NeutrinoOnnxEngine.Phone[] Build(XDocument score) {
            var result = new List<NeutrinoOnnxEngine.Phone>();
            int divisions = 480;
            double bpm = 60, time = 0;
            var notes = new List<(string Lyric, int Tone, double Start, double End)>();
            foreach (var measure in score.Root!.Element("part")!.Elements("measure")) {
                foreach (var element in measure.Elements()) {
                    if (element.Name == "attributes") {
                        divisions = (int?)element.Element("divisions") ?? divisions;
                    } else if (element.Name == "direction") {
                        bpm = (double?)element.Element("sound")?.Attribute("tempo") ?? bpm;
                    } else if (element.Name == "note") {
                        double start = time;
                        time += (int)element.Element("duration")! * 60d / bpm / divisions;
                        if (element.Element("rest") != null) {
                            if (notes.Count > 0 && notes[^1].Lyric.Length == 0) { notes[^1] = ("", 0, notes[^1].Start, time); }
                            else { notes.Add(("", 0, start, time)); }
                            continue;
                        }
                        var pitch = element.Element("pitch")!;
                        int pc = ((string)pitch.Element("step")!) switch {
                            "C" => 0, "D" => 2, "E" => 4, "F" => 5, "G" => 7, "A" => 9, "B" => 11,
                            _ => throw new InvalidDataException("Invalid score pitch."),
                        };
                        int tone = ((int)pitch.Element("octave")! + 1) * 12 + pc + ((int?)pitch.Element("alter") ?? 0);
                        bool continuation = element.Elements("tie").Any(t => (string?)t.Attribute("type") == "stop");
                        if (continuation) {
                            if (notes.Count == 0 || notes[^1].Tone != tone || notes[^1].Lyric.Length == 0) {
                                throw new InvalidDataException("Invalid tied NEUTRINO note.");
                            }
                            var previous = notes[^1];
                            notes[^1] = (previous.Lyric, tone, previous.Start, time);
                        } else {
                            notes.Add(((string?)element.Element("lyric")?.Element("text") ?? "", tone, start, time));
                        }
                    }
                }
            }
            var resolved = new List<(string Lyric, int Tone, double Start, double End)>();
            string? sustained = null;
            foreach (var note in notes) {
                if (note.Lyric == "+") {
                    if (sustained == null || resolved.Count == 0 || Math.Abs(resolved[^1].End - note.Start) > 1e-7) {
                        throw new InvalidDataException("NEUTRINO: '+' precisa seguir uma sílaba cantada sem pausa.");
                    }
                    var previous = resolved[^1];
                    if (previous.Tone == note.Tone) {
                        resolved[^1] = (previous.Lyric, previous.Tone, previous.Start, note.End);
                    } else {
                        resolved.Add((sustained, note.Tone, note.Start, note.End));
                    }
                } else {
                    resolved.Add(note);
                    var symbols = note.Lyric.Length == 0 ? Array.Empty<string>() : ConvertLyric(note.Lyric);
                    sustained = symbols.LastOrDefault() switch {
                        "a" => "あ", "i" => "い", "u" => "う", "e" => "え", "o" => "お", "N" => "ん", _ => null,
                    };
                }
            }
            foreach (var note in resolved) {
                // Native labels quantize elapsed time to 100 ns, shared by all phones of a syllable.
                double start = Math.Floor(note.Start * 1e7) / 1e7;
                double end = Math.Floor(note.End * 1e7) / 1e7;
                var phones = note.Lyric.Length == 0 ? new[] { "pau" } : ConvertLyric(note.Lyric);
                for (int i = 0; i < phones.Length; i++) {
                    result.Add(NeutrinoOnnxEngine.CreatePhone(phones[i], note.Tone, i, start, end));
                }
            }
            return result.ToArray();
        }

        internal static string[] ConvertLyric(string lyric) {
            lyric = lyric.Normalize(System.Text.NormalizationForm.FormC);
            lyric = new string(lyric.Select(c => c >= 'ァ' && c <= 'ヶ' ? (char)(c - 0x60) : c).ToArray());
            string? special = lyric switch {
                "ぁ" => "a", "ぃ" => "i", "ぅ" => "u", "ぇ" => "e", "ぉ" => "o",
                "ゃ" => "y a", "ゅ" => "y u", "ょ" => "y o", "ゎ" => "w a",
                "くぅ" => "k w u", "くゎ" => "k w a", "ぐぅ" => "g w u", "ぐゎ" => "g w a",
                "ゔゃ" => "by a", "ゔゅ" => "by u", "ゔょ" => "by o",
                "でゃ" => "dy a", "でょ" => "dy o", "でぇ" => "dy e",
                "てゃ" => "ty a", "てょ" => "ty o", "しぃ" => "s i",
                _ => null,
            };
            var phones = special?.Split(' ') ?? dictionary.Query(lyric);
            if (phones != null) {
                phones = phones.SelectMany(p => p switch {
                    "kw" => new[] { "k", "w" }, "gw" => new[] { "g", "w" },
                    "A" => new[] { "a" }, "I" => new[] { "i" }, "U" => new[] { "u" },
                    "E" => new[] { "e" }, "O" => new[] { "o" }, _ => new[] { p },
                }).ToArray();
            }
            if (phones == null || phones.Length == 0) {
                throw new InvalidDataException($"NEUTRINO não reconhece a sílaba '{lyric}'. Use uma sílaba japonesa por nota.");
            }
            return phones;
        }
    }
}

