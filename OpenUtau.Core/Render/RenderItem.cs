using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using K4os.Hash.xxHash;
using OpenUtau.Core.Ustx;
using OpenUtau.Core.Util;

namespace OpenUtau.Core.Render {

    internal class RenderItem {

        // For resampler
        public readonly string SourceFile;
        public readonly string SourceTemp;
        public readonly string ResamplerName;

        public readonly int NoteNum;
        public readonly int Velocity;
        public readonly int Volume;
        public readonly string StrFlags;
        public readonly List<int> PitchData;
        public readonly int RequiredLength;
        public readonly int Modulation;
        public readonly double Tempo;
        public readonly UOto Oto;

        // For connector
        public readonly double SkipOver;

        public readonly double PosMs;
        public readonly double DurMs;
        public readonly List<Vector2> Envelope;

        // Sound data
        public byte[] Data;

        // Progress
        public readonly string phonemeName;
        public RenderEngine.Progress progress;

        public Action<byte[]> OnComplete;

        public RenderItem(UPhoneme phoneme, UVoicePart part, UTrack track, UProject project, string resamplerName) {
            SourceFile = phoneme.oto.File;
            SourceFile = Path.Combine(PathManager.Inst.InstalledSingersPath, SourceFile);
            ResamplerName = resamplerName;
            if (project.expressions.TryGetValue("eng", out var descriptor)) {
                int index = (int)phoneme.GetExpression(project, "eng").Item1;
                string resampler = descriptor.options[index];
                if (!string.IsNullOrEmpty(resampler)) {
                    ResamplerName = resampler;
                }
            }
            string ext = Path.GetExtension(SourceFile);
            SourceTemp = Path.Combine(PathManager.Inst.GetCachePath(null),
                $"{HashHex(track.Singer.Id)}-{HashHex(phoneme.oto.Set)}-{HashHex(SourceFile)}{ext}");

            Velocity = (int)phoneme.GetExpression(project, "vel").Item1;
            Volume = (int)phoneme.GetExpression(project, "vol").Item1;
            Modulation = (int)phoneme.GetExpression(project, "mod").Item1;
            var strechRatio = Math.Pow(2, 1.0 - Velocity / 100.0);
            var length = phoneme.oto.Preutter * strechRatio + phoneme.envelope.data[4].X;
            var requiredLength = Math.Ceiling(length / 50 + 1) * 50;
            var lengthAdjustment = phoneme.tailIntrude == 0 ? phoneme.preutter : phoneme.preutter - phoneme.tailIntrude + phoneme.tailOverlap;

            NoteNum = phoneme.Parent.tone;
            StrFlags = phoneme.GetResamplerFlags(project);
            PitchData = BuildPitchData(phoneme, part, project);
            RequiredLength = (int)requiredLength;
            Oto = phoneme.oto;
            Tempo = project.bpm;

            SkipOver = phoneme.oto.Preutter * strechRatio - phoneme.preutter;
            PosMs = project.TickToMillisecond(part.position + phoneme.Parent.position + phoneme.position) - phoneme.preutter;
            DurMs = project.TickToMillisecond(phoneme.Duration) + lengthAdjustment;
            Envelope = phoneme.envelope.data;

            phonemeName = phoneme.phoneme;
        }

        string HashHex(string s) {
            return $"{XXH32.DigestOf(Encoding.UTF8.GetBytes(s)):x8}";
        }

        public uint HashParameters() {
            return XXH32.DigestOf(Encoding.UTF8.GetBytes(ResamplerName + " " + SourceFile + " " + GetResamplerExeArgs()));
        }

        public string GetResamplerExeArgs() {
            // fresamp.exe <infile> <outfile> <tone> <velocity> <flags> <offset> <length_req>
            // <fixed_length> <endblank> <volume> <modulation> <pitch>
            return FormattableString.Invariant($"{MusicMath.GetToneName(NoteNum)} {Velocity:D} \"{StrFlags}\" {Oto.Offset} {RequiredLength:D} {Oto.Consonant} {Oto.Cutoff} {Volume:D} {Modulation:D} T{Tempo} {Base64.Base64EncodeInt12(PitchData.ToArray())}");
        }

        private List<int> BuildPitchData(UPhoneme phoneme, UVoicePart part, UProject project) {
            int leftBound = phoneme.Parent.position + phoneme.position - project.MillisecondToTick(phoneme.preutter);
            int rightBound = phoneme.Parent.position + phoneme.position + phoneme.Duration - project.MillisecondToTick(phoneme.tailIntrude - phoneme.tailOverlap);
            var leftNote = phoneme.Parent;
            var rightNote = phoneme.Parent;
            bool oneMore = true;
            while ((leftBound < leftNote.RightBound || oneMore) && leftNote.Prev != null && leftNote.Prev.End == leftNote.position) {
                leftNote = leftNote.Prev;
                if (leftBound >= leftNote.RightBound) {
                    oneMore = false;
                }
            }
            oneMore = true;
            while ((rightBound > rightNote.LeftBound || oneMore) && rightNote.Next != null && rightNote.Next.position == rightNote.End) {
                rightNote = rightNote.Next;
                if (rightBound <= rightNote.LeftBound) {
                    oneMore = false;
                }
            }

            // Collect pitch curve and vibratos.
            var points = new List<PitchPoint>();
            var vibratos = new List<Tuple<double, double, UVibrato>>();
            var note = leftNote;
            float vel = Velocity;
            var strechRatio = Math.Pow(2, 1.0 - vel / 100);
            float correction = (float)(phoneme.oto.Preutter * (strechRatio - 1));
            while (true) {
                var offsetMs = (float)project.TickToMillisecond(note.position - phoneme.Parent.position);
                foreach (var point in note.pitch.data) {
                    var newpp = point.Clone();
                    newpp.X += offsetMs + correction;
                    newpp.Y -= (phoneme.Parent.tone - note.tone) * 10;
                    points.Add(newpp);
                }
                if (note.vibrato.length != 0) {
                    double vibratoStartMs = project.TickToMillisecond(note.position + note.duration * (1 - note.vibrato.length / 100) - phoneme.Parent.position);
                    double vibratoEndMs = project.TickToMillisecond(note.End - phoneme.Parent.position);
                    vibratos.Add(Tuple.Create(vibratoStartMs, vibratoEndMs, note.vibrato));
                }
                if (note == rightNote) {
                    break;
                }
                note = note.Next;
            }

            // Expand curve if necessary.
            float startMs = (float)(project.TickToMillisecond(phoneme.position) - phoneme.oto.Preutter);
            float endMs = (float)(project.TickToMillisecond(phoneme.End) - phoneme.tailIntrude + phoneme.tailOverlap);
            if (points.First().X > startMs) {
                points.Insert(0, new PitchPoint(startMs, points.First().Y));
            }
            if (points.Last().X < endMs) {
                points.Add(new PitchPoint(endMs, points.Last().Y));
            }

            // Interpolation.
            var pitches = new List<int>();
            const int intervalTick = 5;
            float intervalMs = (float)project.TickToMillisecond(intervalTick);
            float currMs = startMs;
            int i = 0;
            int vibrato = 0;
            while (currMs < endMs) {
                while (points[i + 1].X < currMs) {
                    i++;
                }
                var pit = MusicMath.InterpolateShape(points[i].X, points[i + 1].X, points[i].Y, points[i + 1].Y, currMs, points[i].shape) * 10;
                while (vibrato < vibratos.Count - 1 && vibratos[vibrato].Item2 < currMs) {
                    vibrato++;
                }
                if (vibrato < vibratos.Count && vibratos[vibrato].Item1 <= currMs && currMs < vibratos[vibrato].Item2) {
                    pit += InterpolateVibrato(vibratos[vibrato].Item3, currMs - vibratos[vibrato].Item1, vibratos[vibrato].Item2 - vibratos[vibrato].Item1, project);
                }
                pitches.Add((int)pit);
                currMs += intervalMs;
            }

            return pitches;
        }

        private double InterpolateVibrato(UVibrato vibrato, double posMs, double lengthMs, UProject project) {
            var inMs = lengthMs * vibrato.@in / 100;
            var outMs = lengthMs * vibrato.@out / 100;

            var value = -Math.Sin(2 * Math.PI * (posMs / vibrato.period + vibrato.shift / 100)) * vibrato.depth;

            if (posMs < inMs) {
                value *= posMs / inMs;
            } else if (posMs > lengthMs - outMs) {
                value *= (lengthMs - posMs) / outMs;
            }

            return value;
        }
    }
}
