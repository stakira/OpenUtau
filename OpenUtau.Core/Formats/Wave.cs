using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NAudio.Flac;
using NAudio.Vorbis;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NLayer.NAudioSupport;

namespace OpenUtau.Core.Formats {
    public static class Wave {
        public static Func<string, WaveStream> OverrideMp3Reader;

        public readonly static string kFileFilter = "*.wav;*.mp3;*.ogg;*.flac";

        public static WaveStream OpenFile(string filepath) {
            var ext = Path.GetExtension(filepath);
            byte[] buffer = new byte[4];
            string tag = "";
            using (var stream = File.Open(filepath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                if (stream.CanSeek) {
                    stream.Read(buffer, 0, 4);
                    tag = System.Text.Encoding.UTF8.GetString(buffer);
                }
            }
            if (tag == "RIFF") {
                return new WaveFileReader(filepath);
            }
            if (ext == ".mp3") {
                if (OverrideMp3Reader != null) {
                    return OverrideMp3Reader(filepath);
                }
                return new Mp3FileReaderBase(filepath, wf => new Mp3FrameDecompressor(wf));
            }
            if (tag == "OggS") {
                return new VorbisWaveReader(filepath);
            }
            if (tag == "fLaC") {
                return new FlacReader(filepath);
            }
            throw new Exception("Unsupported audio file format.");
        }

        public static float[] GetSamples(WaveStream waveStream) {
            ISampleProvider provider = waveStream.ToSampleProvider();
            if (provider.WaveFormat.SampleRate != 44100) {
                provider = new WdlResamplingSampleProvider(provider, 44100);
            }
            if (provider.WaveFormat.Channels > 2) {
                provider = provider.ToStereo();
            }
            List<float> samples = new List<float>();
            float[] buffer = new float[128 * 1024];
            int n;
            while ((n = provider.Read(buffer, 0, buffer.Length)) > 0) {
                samples.AddRange(buffer.Take(n));
            }
            return samples.ToArray();
        }

        public static float[] BuildPeaks(WaveStream stream, IProgress<int> progress) {
            const double peaksRate = 4000;
            float[] peaks;
            int channels = stream.WaveFormat.Channels;
            double peaksSamples = (int)((double)stream.Length / stream.WaveFormat.BlockAlign / stream.WaveFormat.SampleRate * peaksRate);
            peaks = new float[(int)(peaksSamples + 1) * channels];
            double blocksPerPixel = stream.Length / stream.WaveFormat.BlockAlign / peaksSamples;

            var sampleProvider = stream.ToSampleProvider();

            float[] buffer = new float[128 * 1024];

            int readPos = 0;
            int peaksPos = 0;
            double bufferPos = 0;
            float lmax = 0, lmin = 0, rmax = 0, rmin = 0;
            int lastProgress = 0;
            int n;
            while ((n = sampleProvider.Read(buffer, 0, buffer.Length)) != 0) {
                readPos += n;
                for (int i = 0; i < n; i += channels) {
                    lmax = Math.Max(lmax, buffer[i]);
                    lmin = Math.Min(lmin, buffer[i]);
                    if (channels > 1) {
                        rmax = Math.Max(rmax, buffer[i + 1]);
                        rmin = Math.Min(rmin, buffer[i + 1]);
                    }
                    if (i > bufferPos) {
                        lmax = -lmax; lmin = -lmin; rmax = -rmax; rmin = -rmin; // negate peaks to fipped waveform
                        peaks[peaksPos * channels] = lmax == 0 ? lmin : lmin == 0 ? lmax : (lmin + lmax) / 2;
                        peaks[peaksPos * channels + 1] = rmax == 0 ? rmin : rmin == 0 ? rmax : (rmin + rmax) / 2;
                        peaksPos++;
                        lmax = lmin = rmax = rmin = 0;
                        bufferPos += blocksPerPixel * stream.WaveFormat.Channels;
                    }
                }
                bufferPos -= n;
                int newProgress = (int)((double)readPos * sizeof(float) * 100 / stream.Length);
                if (newProgress != lastProgress) {
                    progress.Report(newProgress);
                }
            }
            return peaks;
        }
    }
}
