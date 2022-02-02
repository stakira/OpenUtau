using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using K4os.Hash.xxHash;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Core.Render {
    public class RenderPhoneme {
        public readonly int position;
        public readonly int duration;
        public readonly int leading;
        public readonly string phoneme;
        public readonly int tone;

        // classic args
        public readonly string resampler;
        public readonly string infile;
        public readonly string flags;
        public readonly float volume;
        public readonly float velocity;
        public readonly float modulation;

        public readonly uint hash;

        internal RenderPhoneme(UProject project, UTrack track, UVoicePart part, UNote note, UPhoneme phoneme) {
            position = note.position;
            duration = phoneme.Duration;
            leading = (int)Math.Ceiling(project.MillisecondToTick(phoneme.preutter) / 5.0) * 5; // TODO
            this.phoneme = phoneme.phoneme;
            tone = note.tone;

            int eng = (int)phoneme.GetExpression(project, track, "eng").Item1;
            if (project.expressions.TryGetValue("eng", out var descriptor)) {
                if (eng < 0 || eng >= descriptor.options.Length) {
                    eng = 0;
                }
                resampler = descriptor.options[eng]; // TODO: hash default resampler
            }
            //infile = phoneme.oto.File;
            flags = phoneme.GetResamplerFlags(project, track);
            volume = phoneme.GetExpression(project, track, "vol").Item1 * 0.01f;
            velocity = phoneme.GetExpression(project, track, "vel").Item1 * 0.01f;
            modulation = phoneme.GetExpression(project, track, "mod").Item1 * 0.01f;

            hash = Hash();
        }

        public uint Hash() {
            using (var stream = new MemoryStream()) {
                using (var writer = new BinaryWriter(stream)) {
                    writer.Write(duration);
                    writer.Write(phoneme ?? "");
                    writer.Write(tone);

                    writer.Write(resampler ?? "");
                    writer.Write(infile ?? "");
                    writer.Write(flags ?? "");
                    writer.Write(volume);
                    writer.Write(velocity);
                    writer.Write(modulation);
                    return XXH32.DigestOf(stream.ToArray());
                }
            }
        }
    }

    public class RenderPhrase {
        public readonly int position;
        public readonly double tempo;
        public readonly RenderPhoneme[] phones;
        public readonly float[] pitches;

        internal RenderPhrase(UProject project, UTrack track, UVoicePart part, UNote first, UNote last) {
            var notes = new List<UNote>();
            var n = first;
            while (n != last) {
                notes.Add(n);
                n = n.Next;
            }
            notes.Add(last);

            position = part.position;
            tempo = project.bpm;
            var phones = new List<RenderPhoneme>();
            foreach (var note in notes) {
                foreach (var phoneme in note.phonemes) {
                    phones.Add(new RenderPhoneme(project, track, part, note, phoneme));
                }
            }
            this.phones = phones.ToArray();

            const int pitchInterval = 5;
            int pitchStart = phones[0].position - phones[0].leading;
            pitches = new float[(phones.Last().position + phones.Last().duration - pitchStart) / pitchInterval + 1];
            int index = 0;
            foreach (var note in notes) {
                while (pitchStart + index * pitchInterval < note.End) {
                    pitches[index] = note.tone * 100;
                    index++;
                }
            }
            while (index < pitches.Length) {
                pitches[index] = pitches[index - 1];
                index++;
            }
            foreach (var note in notes) {
                if (note.vibrato.length <= 0) {
                    continue;
                }
                int startIndex = Math.Max(0, (note.position - pitchStart) / pitchInterval);
                int endIndex = Math.Min(pitches.Length, (note.End - pitchStart) / pitchInterval);
                for (int i = startIndex; i < endIndex; ++i) {
                    float nPos = (float)(pitchStart + i * pitchInterval - note.position) / note.duration;
                    float nPeriod = (float)project.MillisecondToTick(note.vibrato.period) / note.duration;
                    var point = note.vibrato.Evaluate(nPos, nPeriod, note);
                    pitches[i] = point.Y * 100;
                }
            }
            foreach (var note in notes) {
                var pitchPoints = note.pitch.data
                    .Select(point => new PitchPoint(
                        project.MillisecondToTick(point.X) + note.position,
                        point.Y * 10 + note.tone * 100))
                    .ToList();
                if (pitchPoints.Count == 0) {
                    pitchPoints.Add(new PitchPoint(note.position, note.tone * 100));
                    pitchPoints.Add(new PitchPoint(note.End, note.tone * 100));
                }
                if (note == notes.First() && pitchPoints[0].X > pitchStart) {
                    pitchPoints.Insert(0, new PitchPoint(pitchStart, pitchPoints[0].Y));
                } else if (pitchPoints[0].X > note.position) {
                    pitchPoints.Insert(0, new PitchPoint(note.position, pitchPoints[0].Y));
                }
                if (pitchPoints.Last().X < note.End) {
                    pitchPoints.Add(new PitchPoint(note.End, pitchPoints.Last().Y));
                }
                PitchPoint lastPoint = pitchPoints[0];
                index = Math.Max(0, (int)Math.Ceiling((lastPoint.X - pitchStart) / pitchInterval));
                foreach (var point in pitchPoints.Skip(1)) {
                    int x;
                    while ((x = pitchStart + index * pitchInterval) < point.X && index < pitches.Length) {
                        float pitch = (float)MusicMath.InterpolateShape(lastPoint.X, point.X, lastPoint.Y, point.Y, x, lastPoint.shape);
                        float basePitch = x >= note.position ? note.tone * 100 : (note == first ? note : note.Prev).tone * 100;
                        pitches[index] += pitch - basePitch;
                        index++;
                    }
                    lastPoint = point;
                }
            }
        }

        public static List<RenderPhrase> FromPart(UProject project, UTrack track, UVoicePart part) {
            var result = new List<RenderPhrase>();
            if (part.notes.Count == 0) {
                return result;
            }
            var first = part.notes.First();
            while (first != null) {
                var last = first;
                while (last.Next != null && last.Next.position == last.End) {
                    last = last.Next;
                }
                result.Add(new RenderPhrase(project, track, part, first, last));
                first = last.Next;
            }
            return result;
        }
    }
}
