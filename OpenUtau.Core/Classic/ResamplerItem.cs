using System;
using System.IO;
using System.Linq;
using System.Text;
using K4os.Hash.xxHash;
using NAudio.Wave;
using OpenUtau.Core;
using OpenUtau.Core.Render;

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

        public double offset;
        public double requiredLength;
        public double consonant;
        public double cutoff;
        public double skipOver;

        public double tempo;
        public int[] pitches;

        public ulong hash;

        public ResamplerItem(RenderPhrase phrase, RenderPhone phone) {
            this.phrase = phrase;
            this.phone = phone;

            resampler = Resamplers.GetResampler(
                string.IsNullOrEmpty(phone.resampler)
                    ? WorldlineResampler.name
                    : phone.resampler)
                ?? Resamplers.GetResampler(WorldlineResampler.name);
            inputFile = phone.oto.File;
            inputTemp = VoicebankFiles.GetSourceTempPath(phrase.singer.Id, phone.oto);
            tone = phone.tone;

            flags = phone.flags;
            velocity = (int)(phone.velocity * 100);
            volume = (int)(phone.volume * 100);
            modulation = (int)(phone.modulation * 100);

            int pitchStart = phrase.phones[0].position - phrase.phones[0].leading;

            offset = phone.oto.Offset;
            var stretchRatio = Math.Pow(2, 1.0 - velocity * 0.01);
            double durMs = phone.oto.Preutter * stretchRatio + phone.duration * phrase.tickToMs;
            requiredLength = Math.Ceiling(durMs / 50 + 1) * 50;
            consonant = phone.oto.Consonant;
            cutoff = phone.oto.Cutoff;
            skipOver = phone.oto.Preutter * stretchRatio - phone.preutterMs;

            int pitchLeading = (int)(phone.oto.Preutter * stretchRatio / phrase.tickToMs);
            tempo = phrase.tempo;
            pitches = phrase.pitches
                .Skip((phone.position - pitchLeading - pitchStart) / 5)
                .Take((phone.duration + pitchLeading) / 5)
                .Select(pitch => (int)Math.Round(pitch - phone.tone * 100))
                .ToArray();

            hash = Hash();
            outputFile = Path.Join(PathManager.Inst.CachePath,
                $"res-{XXH32.DigestOf(Encoding.UTF8.GetBytes(phrase.singer.Id)):x8}-{hash:x16}.wav");
        }

        ulong Hash() {
            using (var stream = new MemoryStream()) {
                using (var writer = new BinaryWriter(stream)) {
                    writer.Write(resampler.Name);
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
                    writer.Write(requiredLength);
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
