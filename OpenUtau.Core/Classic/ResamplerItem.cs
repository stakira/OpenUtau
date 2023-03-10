using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using K4os.Hash.xxHash;
using NAudio.Wave;
using OpenUtau.Core;
using OpenUtau.Core.Render;
using OpenUtau.Core.Ustx;
using static OpenUtau.Api.Phonemizer;

namespace OpenUtau.Classic {
    public class ResamplerItem {
        public RenderPhrase phrase;
        public RenderPhone phone;

        public IResampler resampler;
        public string inputFile;
        public string inputTemp;
        public string outputFile;
        public int tone;

        public Tuple<string, int?>[] flags;
        public int velocity;
        public int volume;
        public int modulation;

        public float preutter;
        public float overlap;
        public double offset;
        public double durRequired;
        public double durCorrection;
        public double consonant;
        public double cutoff;
        public double skipOver;

        public double tempo;
        public int[] pitches;

        public ulong hash;

        public ResamplerItem(RenderPhrase phrase, RenderPhone phone) {
            this.phrase = phrase;
            this.phone = phone;

            resampler = ToolsManager.Inst.GetResampler(phone.resampler);
            inputFile = phone.oto.File;
            inputTemp = VoicebankFiles.Inst.GetSourceTempPath(phrase.singer.Id, phone.oto, ".wav");
            tone = phone.tone;

            flags = phone.flags;
            velocity = (int)(phone.velocity * 100);
            volume = (int)(phone.volume * 100);
            modulation = (int)(phone.modulation * 100);

            preutter = (float)phone.preutterMs;
            overlap = (float)phone.overlapMs;
            offset = phone.oto.Offset;
            var stretchRatio = Math.Pow(2, 1.0 - velocity * 0.01);
            double pitchLeadingMs = phone.oto.Preutter * stretchRatio;
            skipOver = phone.oto.Preutter * stretchRatio - phone.leadingMs;
            durRequired = phone.endMs - phone.positionMs + phone.durCorrectionMs + skipOver;
            durRequired = Math.Max(durRequired, phone.oto.Consonant);
            durRequired = Math.Ceiling(durRequired / 50.0 + 0.5) * 50.0;
            durCorrection = phone.durCorrectionMs;
            consonant = phone.oto.Consonant;
            cutoff = phone.oto.Cutoff;

            int leadingTick = phrase.timeAxis.MsPosToTickPos(phone.positionMs - pitchLeadingMs);
            double leadingBpm = phrase.timeAxis.GetBpmAtTick(leadingTick);
         
            int pitchLeading = (int)Math.Ceiling(phrase.timeAxis.TicksBetweenMsPos(phone.positionMs - pitchLeadingMs, phone.positionMs) * (this.phone.adjustedTempo / leadingBpm));
            int pitchSkip = (phrase.leading + phone.position - pitchLeading) / 5;
            int pitchCount = (int)Math.Ceiling(
                (double)phrase.timeAxis.TicksBetweenMsPos(
                    phone.positionMs - pitchLeadingMs,
                    phone.positionMs + phone.envelope[4].X) / 5);
            tempo = phone.adjustedTempo;

            var phrasePitches = phrase.pitches
                .Skip(pitchSkip)
                .Take(pitchCount)
                .Select(pitch => (int)Math.Round(pitch - phone.tone * 100))
                .ToArray();

            pitches = new int[phrasePitches.Length];
            int pitchSkipTempoSection = 0;
            double pitchCountTempoSection = 0;
            for (int i = 0; i < phone.tempos.Length; i++) {
                int tempoStart = Math.Max(phrase.leading + phone.position - pitchLeading, phone.tempos[i].position - phrase.position);
                int tempoEnd = i + 1 < phone.tempos.Length ? phone.tempos[i + 1].position - phrase.position : phrase.timeAxis.MsPosToTickPos(phone.positionMs + phone.envelope[4].X) - phrase.position;
                int tempoLength = tempoEnd - tempoStart;
                int pitchLength = (int)Math.Ceiling(tempoLength / 5.0);
                double tempoRatio = phone.adjustedTempo / phone.tempos[i].bpm;

                int roundedScaledPitchLength = (int)Math.Round(pitchLength * tempoRatio);
                for (int j = 0; j < roundedScaledPitchLength; j++) {
                    int pitchIndex = (int)Math.Floor(j + pitchCountTempoSection);
                    int scaledIndex = (int)Math.Floor(pitchSkipTempoSection + (j / tempoRatio));
                    pitches[pitchIndex] = phrasePitches[scaledIndex];
                }

                pitchSkipTempoSection += pitchLength;
                pitchCountTempoSection += pitchLength * tempoRatio;
            }


            if (pitchSkip < 0) {
                pitches = Enumerable.Repeat(phrasePitches[0], -pitchSkip)
                    .Concat(pitches)
                    .ToArray();
            }

            hash = Hash();
            outputFile = Path.Join(PathManager.Inst.CachePath,
                $"res-{XXH32.DigestOf(Encoding.UTF8.GetBytes(phrase.singer.Id)):x8}-{hash:x16}.wav");
        }
        public string GetFlagsString() {
            var builder = new StringBuilder();
            foreach (var flag in flags) {
                builder.Append(flag.Item1);
                if (flag.Item2.HasValue) {
                    builder.Append(flag.Item2.Value);
                }
            }
            return builder.ToString();
        }

        ulong Hash() {
            using (var stream = new MemoryStream()) {
                using (var writer = new BinaryWriter(stream)) {
                    writer.Write(resampler.ToString());
                    writer.Write(inputFile);
                    writer.Write(tone);

                    foreach (var flag in flags) {
                        writer.Write(flag.Item1);
                        if (flag.Item2.HasValue) {
                            writer.Write(flag.Item2.Value);
                        }
                    }
                    writer.Write(velocity);
                    writer.Write(volume);
                    writer.Write(modulation);

                    writer.Write(offset);
                    writer.Write(durRequired);
                    writer.Write(consonant);
                    writer.Write(cutoff);
                    writer.Write(skipOver);

                    writer.Write(tempo);
                    foreach (int pitch in pitches) {
                        writer.Write(pitch);
                    }
                    return XXH64.DigestOf(stream.ToArray());
                }
            }
        }
    }
}
