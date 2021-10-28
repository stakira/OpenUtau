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
            ResamplerName = resamplerName;
            if (project.expressions.TryGetValue("eng", out var descriptor)) {
                int index = (int)phoneme.GetExpression(project, "eng").Item1;
                if (index < 0 || index >= descriptor.options.Length) {
                    index = 0;
                }
                string resampler = descriptor.options[index];
                if (!string.IsNullOrEmpty(resampler)) {
                    ResamplerName = resampler;
                }
            }
            string ext = Path.GetExtension(SourceFile);
            SourceTemp = Path.Combine(PathManager.Inst.GetCachePath(),
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
            const int intervalTick = 5;
            double intervalMs = project.TickToMillisecond(intervalTick);
            double startMs = project.TickToMillisecond(phoneme.position) - phoneme.oto.Preutter;
            double endMs = project.TickToMillisecond(phoneme.End) - phoneme.tailIntrude + phoneme.tailOverlap;
            double correction = phoneme.oto.Preutter * (Math.Pow(2, 1.0 - Velocity / 100.0) - 1);
            var pitches = new double[(int)((endMs - startMs) / intervalMs)];
            Array.Clear(pitches, 0, pitches.Length);
            var basePitches = new double[pitches.Length];
            Array.Clear(basePitches, 0, basePitches.Length);

            var note = phoneme.Parent;
            int leftBound = note.position + phoneme.position - project.MillisecondToTick(phoneme.preutter);
            int rightBound = note.position + phoneme.position + phoneme.Duration - project.MillisecondToTick(phoneme.tailIntrude - phoneme.tailOverlap);
            var leftNote = note;
            var rightNote = note;
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
            var notes = new List<UNote>();
            while (leftNote != rightNote.Next) {
                notes.Add(leftNote);
                leftNote = leftNote.Next;
            }

            double currMs = startMs;
            int index = 0;
            foreach (var currNote in notes) {
                double noteStartMs = project.TickToMillisecond(currNote.position - note.position);
                double noteEndMs = project.TickToMillisecond(currNote.End - note.position);
                double vibratoStartMs = noteStartMs + project.TickToMillisecond(currNote.duration * (1 - currNote.vibrato.length / 100.0));
                double vibratoLengthMs = noteEndMs - vibratoStartMs;
                while (currMs < noteStartMs && index < pitches.Length) {
                    currMs += intervalMs;
                    index++;
                }
                while (currMs < noteEndMs && index < pitches.Length) {
                    pitches[index] = (currNote.tone - note.tone) * 100;
                    basePitches[index] = pitches[index];
                    if (currMs >= vibratoStartMs) {
                        pitches[index] += InterpolateVibrato(currNote.vibrato, currMs - vibratoStartMs, vibratoLengthMs, project);
                    }
                    currMs += intervalMs;
                    index++;
                }
            }
            foreach (var currNote in notes) {
                double noteStartMs = project.TickToMillisecond(currNote.position - note.position);
                double noteEndMs = project.TickToMillisecond(currNote.End - note.position);
                PitchPoint lastPoint = null;
                foreach (var pp in currNote.pitch.data) {
                    var point = pp.Clone();
                    point.X += (float)(noteStartMs + correction);
                    point.Y = point.Y * 10 + (currNote.tone - note.tone) * 100;
                    if (lastPoint == null && point.X > noteStartMs) {
                        lastPoint = new PitchPoint((float)(noteStartMs - intervalMs), point.Y);
                    }
                    if (lastPoint != null) {
                        currMs = startMs;
                        index = 0;
                        while (currMs < lastPoint.X && index < pitches.Length) {
                            currMs += intervalMs;
                            index++;
                        }
                        while (currMs < point.X && index < pitches.Length) {
                            double pitch = MusicMath.InterpolateShape(lastPoint.X, point.X, lastPoint.Y, point.Y, currMs, lastPoint.shape) - basePitches[index];
                            pitches[index] += pitch;
                            currMs += intervalMs;
                            index++;
                        }
                    }
                    lastPoint = point;
                }
                while (currMs < noteEndMs && index < pitches.Length) {
                    double pitch = lastPoint.Y - basePitches[index];
                    pitches[index] += pitch;
                    currMs += intervalMs;
                    index++;
                }
            }
            return pitches.Select(p => (int)p).ToList();
        }

        private double InterpolateVibrato(UVibrato vibrato, double posMs, double lengthMs, UProject project) {
            var inMs = lengthMs * vibrato.@in / 100;
            var outMs = lengthMs * vibrato.@out / 100;

            var value = Math.Sin(2 * Math.PI * (posMs / vibrato.period + vibrato.shift / 100)) * vibrato.depth;

            if (posMs < inMs) {
                value *= posMs / inMs;
            } else if (posMs > lengthMs - outMs) {
                value *= (lengthMs - posMs) / outMs;
            }

            return value;
        }
    }
}
