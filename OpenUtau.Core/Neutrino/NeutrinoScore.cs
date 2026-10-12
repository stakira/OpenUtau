using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using OpenUtau.Core.Render;

namespace OpenUtau.Core.Neutrino {
    public record NeutrinoScoreNote(string Lyric, int Tone, double PositionMs, double DurationMs);

    public static class NeutrinoScore {
        public const int PaddingMs = 500;

        public static bool IsRestLyric(string lyric) => Regex.IsMatch(lyric.Trim(),
            @"^(?:br(?:_in|_out)?(?:[A-G][#b]?-?\d+)?|breath|R|pau|sil)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        static string NormalizeLyric(string lyric) {
            lyric = lyric.Trim();
            if (IsRestLyric(lyric)) { return ""; }
            if (lyric == "+") { return lyric; }
            if (!Regex.IsMatch(lyric, @"^[\u3041-\u3096\u3099\u309A\u30A1-\u30FA\u30FC]+$")) {
                throw new ArgumentException($"NEUTRINO não reconhece a letra '{lyric}'. Use hiragana ou katakana; aliases UTAU precisam ser convertidos.");
            }
            return lyric;
        }

        // Sinsy/musicXMLtoLabel quantizes durations to its native tick grid.
        // Export the original 480-tick notes and tempo map, not rounded milliseconds.
        public static XDocument Build(RenderPhrase phrase) {
            var first = phrase.notes[0];
            int absoluteStart = phrase.timeAxis.MsPosToTickPos(first.positionMs);
            int absoluteEnd = phrase.timeAxis.MsPosToTickPos(phrase.notes[^1].endMs);
            var tempos = phrase.timeAxis.TemposBetweenTicks(absoluteStart, absoluteEnd)
                .Select(t => (Position: 240 + Math.Max(t.position, absoluteStart) - absoluteStart, Bpm: t.bpm))
                .Append((Position: 240 + phrase.notes[^1].end - first.position, Bpm: 60d))
                .GroupBy(t => t.Position).Select(g => g.Last()).OrderBy(t => t.Position).ToArray();
            return BuildCore(phrase.notes.Select(n => new NeutrinoScoreNote(n.lyric, n.tone,
                n.position - first.position, n.duration)), 480, 240, tempos);
        }

        // Encode absolute elapsed time at 60 BPM, 1000 divisions per quarter.
        // This preserves tempo changes in the source rather than approximating one tempo.
        public static XDocument Build(IEnumerable<NeutrinoScoreNote> input)
            => BuildCore(input, 1000, PaddingMs, Array.Empty<(int Position, double Bpm)>());

        static XDocument BuildCore(IEnumerable<NeutrinoScoreNote> input, int divisions, int padding,
                (int Position, double Bpm)[] tempos) {
            int barUnits = 4 * divisions, tempoIndex = 0;
            var notes = input.ToArray();
            if (notes.Length == 0) { throw new ArgumentException("NEUTRINO needs at least one note."); }
            var part = new XElement("part", new XAttribute("id", "P1"));
            XElement? measure = null;
            int measureNumber = 0, cursor = 0;
            void AddSegment(int duration, int? tone, string? lyric) {
                bool continuing = false;
                while (duration > 0) {
                    if (measure == null || cursor % barUnits == 0) {
                        measure = new XElement("measure", new XAttribute("number", ++measureNumber));
                        part.Add(measure);
                        if (measureNumber == 1) {
                            measure.Add(new XElement("attributes",
                                new XElement("divisions", divisions),
                                new XElement("time", new XElement("beats", 4), new XElement("beat-type", 4))));
                            measure.Add(new XElement("direction", new XElement("sound", new XAttribute("tempo", 60))));
                        }
                    }
                    while (tempoIndex < tempos.Length && tempos[tempoIndex].Position == cursor) {
                        measure!.Add(new XElement("direction", new XElement("sound", new XAttribute("tempo", tempos[tempoIndex++].Bpm))));
                    }
                    int count = Math.Min(duration, barUnits - cursor % barUnits);
                    if (tempoIndex < tempos.Length && tempos[tempoIndex].Position > cursor) {
                        count = Math.Min(count, tempos[tempoIndex].Position - cursor);
                    }
                    var note = new XElement("note");
                    if (tone.HasValue) {
                        int pc = tone.Value % 12;
                        string[] steps = { "C", "C", "D", "D", "E", "F", "F", "G", "G", "A", "A", "B" };
                        var pitch = new XElement("pitch", new XElement("step", steps[pc]));
                        if (new[] { 1, 3, 6, 8, 10 }.Contains(pc)) { pitch.Add(new XElement("alter", 1)); }
                        pitch.Add(new XElement("octave", tone.Value / 12 - 1));
                        note.Add(pitch);
                    } else { note.Add(new XElement("rest")); }
                    note.Add(new XElement("duration", count));
                    if (tone.HasValue) {
                        if (continuing) { note.Add(new XElement("tie", new XAttribute("type", "stop"))); }
                        if (count < duration) { note.Add(new XElement("tie", new XAttribute("type", "start"))); }
                        if (!continuing) { note.Add(new XElement("lyric", new XElement("text", lyric))); }
                        if (continuing || count < duration) {
                            var notation = new XElement("notations");
                            if (continuing) { notation.Add(new XElement("tied", new XAttribute("type", "stop"))); }
                            if (count < duration) { notation.Add(new XElement("tied", new XAttribute("type", "start"))); }
                            note.Add(notation);
                        }
                    }
                    measure!.Add(note);
                    cursor += count;
                    duration -= count;
                    continuing = true;
                }
            }
            AddSegment(padding, null, null);
            double origin = notes[0].PositionMs;
            bool canExtend = false;
            foreach (var note in notes) {
                if (note.Tone < 0 || note.Tone > 127 || note.DurationMs <= 0) {
                    throw new ArgumentException("Invalid NEUTRINO note pitch or duration.");
                }
                string lyric = NormalizeLyric(note.Lyric);
                int start = padding + (int)Math.Round(note.PositionMs - origin);
                if (start < cursor) { throw new ArgumentException("Overlapping notes cannot be rendered by NEUTRINO."); }
                if (lyric == "+" && (!canExtend || start != cursor)) {
                    throw new ArgumentException("NEUTRINO: '+' precisa seguir uma sílaba cantada sem pausa entre as notas.");
                }
                canExtend = lyric.Length != 0;
                AddSegment(start - cursor, null, null);
                int end = padding + (int)Math.Round(note.PositionMs + note.DurationMs - origin);
                AddSegment(Math.Max(1, end - start), lyric.Length == 0 ? null : note.Tone, lyric);
            }
            AddSegment(padding, null, null);
            // Complete the final measure so musicXMLtoLabel sees a valid score.
            if (cursor % barUnits != 0) { AddSegment(barUnits - cursor % barUnits, null, null); }
            return new XDocument(new XDeclaration("1.0", "utf-8", null),
                new XDocumentType("score-partwise", "-//Recordare//DTD MusicXML 3.1 Partwise//EN", "http://www.musicxml.org/dtds/partwise.dtd", null),
                new XElement("score-partwise", new XAttribute("version", "3.1"),
                    new XElement("part-list", new XElement("score-part", new XAttribute("id", "P1"), new XElement("part-name", "Voice"))), part));
        }
    }
}

