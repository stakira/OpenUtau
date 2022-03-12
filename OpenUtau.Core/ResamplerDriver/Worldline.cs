using System;
using System.Linq;
using System.Runtime.InteropServices;
using NAudio.Wave;
using Serilog;

namespace OpenUtau.Core.ResamplerDriver {
    static class Worldline {
        [StructLayout(LayoutKind.Sequential)]
        public struct SynthRequest {
            public int sample_fs;
            public int sample_length;
            public IntPtr sample;
            public int tone;
            public double con_vel;
            public double offset;
            public double required_length;
            public double consonant;
            public double cut_off;
            public double volume;
            public double modulation;
            public double tempo;
            public int pitch_bend_length;
            public IntPtr pitch_bend;
            public int flag_g;
            public int flag_Mt;
            public int flag_O;
            public int flag_P;
        };

        /*
        [DllImport("worldline")]
        static extern int DecodeAndSynthesis(
            double[] f0, int f0Length,
            double[,] mgc, int mgcSize,
            double[,] bap, int fftSize,
            double framePeriod, int fs,
            int yLength, ref double[] y);
        */

        [DllImport("worldline")]
        static extern int Resample(IntPtr request, ref IntPtr y);

        public static float[] Resample(DriverModels.EngineInput args, ILogger logger) {
            int fs;
            double[] sample;
            using (var waveStream = Formats.Wave.OpenFile(args.inputWaveFile)) {
                fs = waveStream.WaveFormat.SampleRate;
                sample = Formats.Wave.GetSamples(waveStream.ToSampleProvider().ToMono(1, 0))
                    .Select(f => (double)f).ToArray();
            }

            var pinnedSample = GCHandle.Alloc(sample, GCHandleType.Pinned);
            var pinnedPitchBend = GCHandle.Alloc(args.pitchBend, GCHandleType.Pinned);
            var request = new SynthRequest {
                sample_fs = fs,
                sample_length = sample.Length,
                sample = pinnedSample.AddrOfPinnedObject(),
                tone = MusicMath.NameToTone(args.NoteString),
                con_vel = args.Velocity,
                offset = args.Offset,
                required_length = args.RequiredLength,
                consonant = args.Consonant,
                cut_off = args.Cutoff,
                volume = args.Volume,
                modulation = args.Modulation,
                tempo = args.Tempo,
                pitch_bend_length = args.nPitchBend,
                pitch_bend = pinnedPitchBend.AddrOfPinnedObject(),
                flag_g = 0,
                flag_Mt = 0,
                flag_O = 0,
                flag_P = 86,
            };

            try {
                unsafe {
                    IntPtr buffer = IntPtr.Zero;
                    int size = Resample(new IntPtr(&request), ref buffer);
                    var data = new float[size];
                    Marshal.Copy(buffer, data, 0, size);
                    Marshal.FreeCoTaskMem(buffer);
                    return data;
                }
            } finally {
                pinnedSample.Free();
                pinnedPitchBend.Free();
            }
        }
    }
}
