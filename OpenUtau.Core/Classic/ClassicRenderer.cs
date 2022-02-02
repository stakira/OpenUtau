using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using K4os.Hash.xxHash;
using NAudio.Wave;
using OpenUtau.Core;
using OpenUtau.Core.Render;
using OpenUtau.Core.ResamplerDriver;

namespace OpenUtau.Classic {
    public class ClassicRenderer : IRenderer {
        class ResamplerItem {
            public RenderPhrase phrase;
            public RenderPhone phone;

            public IResamplerDriver resampler;
            public string inputFile;
            public string inputTemp;
            public string outputFile;
            public string tone;

            public string flags;
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

                resampler = ResamplerDrivers.GetResampler(
                    string.IsNullOrEmpty(phone.resampler)
                        ? ResamplerDrivers.GetDefaultResamplerName()
                        : phone.resampler);
                inputFile = phone.oto.File;
                inputTemp = VoicebankFiles.GetSourceTempPath(phrase.singerId, phone.oto);
                tone = MusicMath.GetToneName(phone.tone);

                flags = phone.flags;
                velocity = (int)(phone.velocity * 100);
                volume = (int)(phone.volume * 100);
                modulation = (int)(phone.modulation * 100);

                int pitchStart = phrase.phones[0].position - phrase.phones[0].leading;

                offset = phone.oto.Offset;
                double durMs = phone.duration * phrase.tickToMs;
                requiredLength = Math.Ceiling(durMs / 50 + 1) * 50;
                consonant = phone.oto.Consonant;
                cutoff = phone.oto.Cutoff;
                var stretchRatio = Math.Pow(2, 1.0 - velocity);
                skipOver = phone.oto.Preutter * stretchRatio - phone.preutterMs;

                tempo = phrase.tempo;
                pitches = phrase.pitches
                    .Skip((phone.position - phone.leading - pitchStart) / 5)
                    .Take((phone.duration + phone.leading) / 5)
                    .Select(pitch => (int)Math.Round(pitch - phone.tone * 100))
                    .ToArray();

                hash = Hash();
                outputFile = Path.Join(PathManager.Inst.CachePath,
                    $"res-{XXH32.DigestOf(Encoding.UTF8.GetBytes(phrase.singerId)):x8}-{hash:x16}.wav");
            }

            ulong Hash() {
                using (var stream = new MemoryStream()) {
                    using (var writer = new BinaryWriter(stream)) {
                        writer.Write(resampler.Name);
                        writer.Write(inputFile);
                        writer.Write(tone);

                        writer.Write(flags);
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

        public Task<float[]> Render(RenderPhrase phrase, CancellationTokenSource cancellation) {
            var resamplerItems = new List<ResamplerItem>();
            foreach (var phone in phrase.phones) {
                resamplerItems.Add(new ResamplerItem(phrase, phone));
            }
            var task = Task.Run(() => {
                Parallel.ForEach(source: resamplerItems, parallelOptions: new ParallelOptions() {
                    MaxDegreeOfParallelism = 2
                }, body: item => {
                    if (cancellation.IsCancellationRequested) {
                        return;
                    }
                    var engineInput = new DriverModels.EngineInput() {
                        inputWaveFile = item.inputTemp,
                        outputWaveFile = item.outputFile,
                        NoteString = item.tone,
                        Velocity = item.velocity,
                        StrFlags = item.flags,
                        Offset = item.offset,
                        RequiredLength = item.requiredLength,
                        Consonant = item.consonant,
                        Cutoff = item.cutoff,
                        Volume = item.volume,
                        Modulation = item.modulation,
                        pitchBend = item.pitches,
                        nPitchBend = item.pitches.Length,
                        Tempo = item.tempo,
                    };
                    item.resampler.DoResamplerReturnsFile(engineInput, Serilog.Log.Logger);
                });
                var samples = Concatenate(resamplerItems, cancellation);
                foreach (var item in resamplerItems) {
                    if (File.Exists(item.outputFile)) {
                        //File.Delete(item.outputFile);
                    }
                }
                return samples;
            });
            return task;
        }

        float[] Concatenate(List<ResamplerItem> resamplerItems, CancellationTokenSource cancellation) {
            string tmpFile = Path.GetTempFileName();
            foreach (var item in resamplerItems) {
                if (cancellation.IsCancellationRequested) {
                    return null;
                }
                //using (var proc = new Process()) {
                //    proc.StartInfo = new ProcessStartInfo(FilePath, ArgParam) {
                //        UseShellExecute = false,
                //        CreateNoWindow = true,
                //    };
                //}
            }
            float[] samples = new float[0]; // Core.Formats.Wave.GetSamples(tmpFile);
            //File.Delete(tmpFile);
            return samples;
        }
    }
}
