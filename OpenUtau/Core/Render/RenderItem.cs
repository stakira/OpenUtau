using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using OpenUtau.Core.USTx;
using OpenUtau.Core.Util;
using OpenUtau.SimpleHelpers;
using xxHashSharp;

namespace OpenUtau.Core.Render {

    internal class RenderItem {

        // For resampler
        public string SourceFile;

        public int NoteNum;
        public int Velocity;
        public int Volume;
        public string StrFlags;
        public List<int> PitchData;
        public int RequiredLength;
        public int Modulation;
        public double Tempo;
        public UOto Oto;

        // For connector
        public double SkipOver;

        public double PosMs;
        public double DurMs;
        public List<ExpPoint> Envelope;

        // Sound data
        public MemorySampleProvider Sound;

        // Progress
        public string phonemeName;
        public RenderEngine.Progress progress;

        public RenderItem(UPhoneme phoneme, UVoicePart part, UProject project) {
            var singer = project.Tracks[part.TrackNo].Singer;
            SourceFile = FileEncoding.ConvertEncoding(singer.FileEncoding, singer.PathEncoding, phoneme.Oto.File);
            SourceFile = Path.Combine(singer.Path, SourceFile);

            var strechRatio = Math.Pow(2, 1.0 - (double)(int)phoneme.Parent.Expressions["velocity"].Data / 100);
            var length = phoneme.Oto.Preutter * strechRatio + phoneme.Envelope.Points[4].X;
            var requiredLength = Math.Ceiling(length / 50 + 1) * 50;
            var lengthAdjustment = phoneme.TailIntrude == 0 ? phoneme.Preutter : phoneme.Preutter - phoneme.TailIntrude + phoneme.TailOverlap;

            NoteNum = phoneme.Parent.NoteNum;
            Velocity = (int)phoneme.Parent.Expressions["velocity"].Data;
            Volume = (int)phoneme.Parent.Expressions["volume"].Data;
            StrFlags = phoneme.Parent.GetResamplerFlags();
            PitchData = BuildPitchData(phoneme, part, project);
            RequiredLength = (int)requiredLength;
            Oto = phoneme.Oto;
            Tempo = project.BPM;

            SkipOver = phoneme.Oto.Preutter * strechRatio - phoneme.Preutter;
            PosMs = project.TickToMillisecond(part.PosTick + phoneme.Parent.PosTick + phoneme.PosTick) - phoneme.Preutter;
            DurMs = project.TickToMillisecond(phoneme.DurTick) + lengthAdjustment;
            Envelope = phoneme.Envelope.Points;

            phonemeName = phoneme.Phoneme;
        }

        public uint HashParameters() {
            return xxHash.CalculateHash(Encoding.UTF8.GetBytes(SourceFile + " " + GetResamplerExeArgs()));
        }

        public string GetResamplerExeArgs() {
            // fresamp.exe <infile> <outfile> <tone> <velocity> <flags> <offset> <length_req>
            // <fixed_length> <endblank> <volume> <modulation> <pitch>
            return FormattableString.Invariant($"{MusicMath.GetNoteString(NoteNum)} {Velocity:D} \"{StrFlags}\" {Oto.Offset} {RequiredLength:D} {Oto.Consonant} {Oto.Cutoff} {Volume:D} {Modulation:D} {Tempo} {Base64.Base64EncodeInt12(PitchData.ToArray())}");
        }

        public ISampleProvider GetSampleProvider() {
            var envelopeSampleProvider = new EnvelopeSampleProvider(Sound, Envelope, SkipOver);
            var sampleRate = Sound.WaveFormat.SampleRate;
            return new OffsetSampleProvider(envelopeSampleProvider) {
                DelayBySamples = (int)(PosMs * sampleRate / 1000),
                TakeSamples = (int)(DurMs * sampleRate / 1000),
                SkipOverSamples = (int)(SkipOver * sampleRate / 1000),
            };
        }

        private List<int> BuildPitchData(UPhoneme phoneme, UVoicePart part, UProject project) {
            var pitches = new List<int>();
            var lastNote = part.Notes.OrderByDescending(x => x).Where(x => x.CompareTo(phoneme.Parent) < 0).FirstOrDefault();
            var nextNote = part.Notes.Where(x => x.CompareTo(phoneme.Parent) > 0).FirstOrDefault();
            // Get relevant pitch points
            var pps = new List<PitchPoint>();

            var lastNoteInvolved = lastNote != null && phoneme.Overlapped;
            var nextNoteInvolved = nextNote != null && nextNote.Phonemes[0].Overlapped;

            double lastVibratoStartMs = 0;
            double lastVibratoEndMs = 0;
            double vibratoStartMs = 0;
            double vibratoEndMs = 0;

            if (lastNoteInvolved) {
                var offsetMs = DocManager.Inst.Project.TickToMillisecond(phoneme.Parent.PosTick - lastNote.PosTick);
                foreach (var pp in lastNote.PitchBend.Points) {
                    var newpp = pp.Clone();
                    newpp.X -= offsetMs;
                    newpp.Y -= (phoneme.Parent.NoteNum - lastNote.NoteNum) * 10;
                    pps.Add(newpp);
                }
                if (lastNote.Vibrato.Depth != 0) {
                    lastVibratoStartMs = -DocManager.Inst.Project.TickToMillisecond(lastNote.DurTick) * lastNote.Vibrato.Length / 100;
                    lastVibratoEndMs = 0;
                }
            }

            foreach (var pp in phoneme.Parent.PitchBend.Points) {
                pps.Add(pp);
            }

            if (phoneme.Parent.Vibrato.Depth != 0) {
                vibratoEndMs = DocManager.Inst.Project.TickToMillisecond(phoneme.Parent.DurTick);
                vibratoStartMs = vibratoEndMs * (1 - phoneme.Parent.Vibrato.Length / 100);
            }

            if (nextNoteInvolved) {
                var offsetMs = DocManager.Inst.Project.TickToMillisecond(phoneme.Parent.PosTick - nextNote.PosTick);
                foreach (var pp in nextNote.PitchBend.Points) {
                    var newpp = pp.Clone();
                    newpp.X -= offsetMs;
                    newpp.Y -= (phoneme.Parent.NoteNum - nextNote.NoteNum) * 10;
                    pps.Add(newpp);
                }
            }

            var startMs = DocManager.Inst.Project.TickToMillisecond(phoneme.PosTick) - phoneme.Oto.Preutter;
            var endMs = DocManager.Inst.Project.TickToMillisecond(phoneme.DurTick) -
                (nextNote != null && nextNote.Phonemes[0].Overlapped ? nextNote.Phonemes[0].Preutter - nextNote.Phonemes[0].Overlap : 0);
            if (pps.Count > 0) {
                if (pps.First().X > startMs) {
                    pps.Insert(0, new PitchPoint(startMs, pps.First().Y));
                }

                if (pps.Last().X < endMs) {
                    pps.Add(new PitchPoint(endMs, pps.Last().Y));
                }
            } else {
                throw new Exception("Zero pitch points.");
            }

            // Interpolation
            const int intervalTick = 5;
            var intervalMs = DocManager.Inst.Project.TickToMillisecond(intervalTick);
            var currMs = startMs;
            var i = 0;

            while (currMs < endMs) {
                while (pps[i + 1].X < currMs) {
                    i++;
                }

                var pit = MusicMath.InterpolateShape(pps[i].X, pps[i + 1].X, pps[i].Y, pps[i + 1].Y, currMs, pps[i].Shape);
                pit *= 10;

                // Apply vibratos
                if (currMs < lastVibratoEndMs && currMs >= lastVibratoStartMs) {
                    pit += InterpolateVibrato(lastNote.Vibrato, currMs - lastVibratoStartMs);
                }

                if (currMs < vibratoEndMs && currMs >= vibratoStartMs) {
                    pit += InterpolateVibrato(phoneme.Parent.Vibrato, currMs - vibratoStartMs);
                }

                pitches.Add((int)pit);
                currMs += intervalMs;
            }

            return pitches;
        }

        private double InterpolateVibrato(VibratoExpression vibrato, double posMs) {
            var lengthMs = vibrato.Length / 100 * DocManager.Inst.Project.TickToMillisecond(vibrato.Parent.DurTick);
            var inMs = lengthMs * vibrato.In / 100;
            var outMs = lengthMs * vibrato.Out / 100;

            var value = -Math.Sin(2 * Math.PI * (posMs / vibrato.Period + vibrato.Shift / 100)) * vibrato.Depth;

            if (posMs < inMs) {
                value *= posMs / inMs;
            } else if (posMs > lengthMs - outMs) {
                value *= (lengthMs - posMs) / outMs;
            }

            return value;
        }
    }
}
