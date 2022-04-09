using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using K4os.Hash.xxHash;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Core.Render {
    public class RenderNote {
        public readonly string lyric;
        public readonly int position;
        public readonly int duration;
        public readonly int tone;

        public RenderNote(UNote note) {
            lyric = note.lyric;
            position = note.position;
            duration = note.duration;
            tone = note.tone;
        }
    }

    public class RenderPhone {
        public readonly int position;
        public readonly int duration;
        public readonly int leading;
        public readonly string phoneme;
        public readonly int tone;
        public readonly int noteIndex;

        // classic args
        public readonly string resampler;
        public readonly Tuple<string, int?>[] flags;
        public readonly float volume;
        public readonly float velocity;
        public readonly float modulation;
        public readonly float preutterMs;
        public readonly Vector2[] envelope;

        public readonly UOto oto;
        public readonly ulong hash;

        internal RenderPhone(UProject project, UTrack track, UVoicePart part, UNote note, UPhoneme phoneme) {
            position = note.position + phoneme.position;
            duration = phoneme.Duration;
            leading = (int)Math.Round(project.MillisecondToTick(phoneme.preutter) / 5.0) * 5; // TODO
            this.phoneme = phoneme.phoneme;
            tone = note.tone;

            int eng = (int)phoneme.GetExpression(project, track, Format.Ustx.ENG).Item1;
            if (project.expressions.TryGetValue(Format.Ustx.ENG, out var descriptor)) {
                if (eng < 0 || eng >= descriptor.options.Length) {
                    eng = 0;
                }
                resampler = descriptor.options[eng];
                if (string.IsNullOrEmpty(resampler)) {
                    resampler = Util.Preferences.Default.Resampler;
                }
            }
            flags = phoneme.GetResamplerFlags(project, track);
            volume = phoneme.GetExpression(project, track, Format.Ustx.VOL).Item1 * 0.01f;
            velocity = phoneme.GetExpression(project, track, Format.Ustx.VEL).Item1 * 0.01f;
            modulation = phoneme.GetExpression(project, track, Format.Ustx.MOD).Item1 * 0.01f;
            preutterMs = phoneme.preutter;
            envelope = phoneme.envelope.data.ToArray();

            oto = phoneme.oto;
            hash = Hash();
        }

        private ulong Hash() {
            using (var stream = new MemoryStream()) {
                using (var writer = new BinaryWriter(stream)) {
                    writer.Write(duration);
                    writer.Write(phoneme ?? "");
                    writer.Write(tone);

                    writer.Write(resampler ?? "");
                    foreach (var flag in flags) {
                        writer.Write(flag.Item1);
                        if (flag.Item2.HasValue) {
                            writer.Write(flag.Item2.Value);
                        }
                    }
                    writer.Write(volume);
                    writer.Write(velocity);
                    writer.Write(modulation);
                    writer.Write(preutterMs);
                    return XXH64.DigestOf(stream.ToArray());
                }
            }
        }
    }

    public class RenderPhrase {
        public readonly string singerId;
        public readonly USinger singer;
        public readonly int position;
        public readonly double tempo;
        public readonly double tickToMs;
        public readonly RenderNote[] notes;
        public readonly RenderPhone[] phones;
        public readonly int pitchStart;
        public readonly float[] pitches;
        public readonly float[] pitchesBeforeDeviation;
        public readonly float[] dynamics;
        public readonly float[] gender;
        public readonly float[] breathiness;
        public readonly float[] toneShift;
        public readonly float[] tension;
        public readonly float[] voicing;
        public readonly ulong hash;

        internal readonly IRenderer renderer;

        internal RenderPhrase(UProject project, UTrack track, UVoicePart part, IEnumerable<UPhoneme> phonemes) {
            var uNotes = new List<UNote>();
            uNotes.Add(phonemes.First().Parent);
            var endNote = phonemes.Last().Parent;
            while (endNote.Next != null && endNote.Next.Extends != null) {
                endNote = endNote.Next;
            }
            while (uNotes.Last() != endNote) {
                uNotes.Add(uNotes.Last().Next);
            }
            var tail = uNotes.Last();
            var next = tail.Next;
            while (next != null && next.Extends == tail) {
                uNotes.Add(next);
                next = next.Next;
            }
            notes = uNotes
                .Select(n => new RenderNote(n))
                .ToArray();
            phones = phonemes
                .Select(p => new RenderPhone(project, track, part, p.Parent, p))
                .ToArray();

            singerId = track.Singer.Id;
            singer = track.Singer;
            renderer = track.Renderer;
            position = part.position;
            tempo = project.bpm;
            tickToMs = 60000.0 / project.bpm * project.beatUnit / 4 / project.resolution;

            const int pitchInterval = 5;
            pitchStart = phones[0].position - phones[0].leading;
            pitches = new float[(phones.Last().position + phones.Last().duration - pitchStart) / pitchInterval + 1];
            int index = 0;
            foreach (var note in uNotes) {
                while (pitchStart + index * pitchInterval < note.End && index < pitches.Length) {
                    pitches[index] = note.tone * 100;
                    index++;
                }
            }
            while (index < pitches.Length) {
                pitches[index] = pitches[index - 1];
                index++;
            }
            foreach (var note in uNotes) {
                if (note.vibrato.length <= 0) {
                    continue;
                }
                int startIndex = Math.Max(0, (int)Math.Ceiling((float)(note.position - pitchStart) / pitchInterval));
                int endIndex = Math.Min(pitches.Length, (note.End - pitchStart) / pitchInterval);
                for (int i = startIndex; i < endIndex; ++i) {
                    float nPos = (float)(pitchStart + i * pitchInterval - note.position) / note.duration;
                    float nPeriod = (float)project.MillisecondToTick(note.vibrato.period) / note.duration;
                    var point = note.vibrato.Evaluate(nPos, nPeriod, note);
                    pitches[i] = point.Y * 100;
                }
            }
            foreach (var note in uNotes) {
                var pitchPoints = note.pitch.data
                    .Select(point => new PitchPoint(
                        project.MillisecondToTick(point.X) + note.position,
                        point.Y * 10 + note.tone * 100,
                        point.shape))
                    .ToList();
                if (pitchPoints.Count == 0) {
                    pitchPoints.Add(new PitchPoint(note.position, note.tone * 100));
                    pitchPoints.Add(new PitchPoint(note.End, note.tone * 100));
                }
                if (note == uNotes.First() && pitchPoints[0].X > pitchStart) {
                    pitchPoints.Insert(0, new PitchPoint(pitchStart, pitchPoints[0].Y));
                } else if (pitchPoints[0].X > note.position) {
                    pitchPoints.Insert(0, new PitchPoint(note.position, pitchPoints[0].Y));
                }
                if (pitchPoints.Last().X < note.End) {
                    pitchPoints.Add(new PitchPoint(note.End, pitchPoints.Last().Y));
                }
                PitchPoint lastPoint = pitchPoints[0];
                index = Math.Max(0, (int)((lastPoint.X - pitchStart) / pitchInterval));
                foreach (var point in pitchPoints.Skip(1)) {
                    int x = pitchStart + index * pitchInterval;
                    while (x < point.X && index < pitches.Length) {
                        float pitch = (float)MusicMath.InterpolateShape(lastPoint.X, point.X, lastPoint.Y, point.Y, x, lastPoint.shape);
                        float basePitch = note.Prev != null && x < note.Prev.End
                            ? note.Prev.tone * 100
                            : note.tone * 100;
                        pitches[index] += pitch - basePitch;
                        index++;
                        x += pitchInterval;
                    }
                    lastPoint = point;
                }
            }

            pitchesBeforeDeviation = pitches.ToArray();
            var curve = part.curves.FirstOrDefault(c => c.abbr == Format.Ustx.PITD);
            if (curve != null && !curve.IsEmpty) {
                for (int i = 0; i < pitches.Length; ++i) {
                    pitches[i] += curve.Sample(pitchStart + i * pitchInterval);
                }
            }

            dynamics = SampleCurve(part, Format.Ustx.DYN, pitchStart, pitches.Length,
                (x, c) => x == c.descriptor.min
                    ? 0
                    : (float)MusicMath.DecibelToLinear(x * 0.1));
            toneShift = SampleCurve(part, Format.Ustx.SHFC, pitchStart, pitches.Length, (x, _) => x);
            gender = SampleCurve(part, Format.Ustx.GENC, pitchStart, pitches.Length, (x, _) => x);
            tension = SampleCurve(part, Format.Ustx.TENC, pitchStart, pitches.Length, (x, _) => x);
            breathiness = SampleCurve(part, Format.Ustx.BREC, pitchStart, pitches.Length, (x, _) => x);
            voicing = SampleCurve(part, Format.Ustx.VOIC, pitchStart, pitches.Length, (x, _) => x);

            hash = Hash();
        }

        private static float[] SampleCurve(UVoicePart part, string abbr, int start, int length, Func<float, UCurve, float> convert) {
            const int interval = 5;
            var curve = part.curves.FirstOrDefault(c => c.abbr == abbr);
            if (curve == null || curve.IsEmptyBetween(
                start, start + (length - 1) * interval, (int)curve.descriptor.defaultValue)) {
                return null;
            }
            var result = new float[length];
            for (int i = 0; i < length; ++i) {
                result[i] = convert(curve.Sample(start + i * interval), curve);
            }
            return result;
        }

        private ulong Hash() {
            using (var stream = new MemoryStream()) {
                using (var writer = new BinaryWriter(stream)) {
                    writer.Write(singerId);
                    writer.Write(tempo);
                    writer.Write(tickToMs);
                    foreach (var phone in phones) {
                        writer.Write(phone.hash);
                    }
                    foreach (var array in new float[][] { pitches, dynamics, gender, breathiness, toneShift, tension, voicing }) {
                        if (array == null) {
                            writer.Write("null");
                        } else {
                            foreach (var v in array) {
                                writer.Write(v);
                            }
                        }
                    }
                    return XXH64.DigestOf(stream.ToArray());
                }
            }
        }

        public static List<RenderPhrase> FromPart(UProject project, UTrack track, UVoicePart part) {
            var phrases = new List<RenderPhrase>();
            var phonemes = part.notes
                .Where(note => !note.OverlapError)
                .SelectMany(note => note.phonemes.Where(phoneme => !phoneme.Error))
                .ToList();
            if (phonemes.Count == 0) {
                return phrases;
            }
            var phrasePhonemes = new List<UPhoneme>() { phonemes[0] };
            for (int i = 1; i < phonemes.Count; ++i) {
                if (phonemes[i - 1].Parent.position + phonemes[i - 1].End != phonemes[i].Parent.position + phonemes[i].position) {
                    phrases.Add(new RenderPhrase(project, track, part, phrasePhonemes));
                    phrasePhonemes.Clear();
                }
                phrasePhonemes.Add(phonemes[i]);
            }
            if (phrasePhonemes.Count > 0) {
                phrases.Add(new RenderPhrase(project, track, part, phrasePhonemes));
                phrasePhonemes.Clear();
            }
            return phrases;
        }
    }
}
