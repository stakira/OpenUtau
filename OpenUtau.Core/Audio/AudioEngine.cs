using System;
using System.Runtime.InteropServices;
using OpenUtau.Audio.Bindings;

namespace OpenUtau.Audio {
    class AudioEngine {
        private const int FramesPerBuffer = 0; // paFramesPerBufferUnspecified
        private const PaBinding.PaStreamFlags StreamFlags = PaBinding.PaStreamFlags.paNoFlag;
        private readonly int channels;
        private readonly int sampleRate;
        private readonly double latency;
        private readonly IntPtr stream;
        private bool disposed;

        public readonly AudioDevice device;

        public AudioEngine(AudioDevice device, int channels, int sampleRate, double latency) {
            this.device = device;
            this.channels = channels;
            this.sampleRate = sampleRate;
            this.latency = latency;

            var parameters = new PaBinding.PaStreamParameters {
                channelCount = channels,
                device = device.DeviceIndex,
                hostApiSpecificStreamInfo = IntPtr.Zero,
                sampleFormat = PaBinding.PaSampleFormat.paFloat32,
                suggestedLatency = latency
            };

            IntPtr stream;

            unsafe {
                PaBinding.PaStreamParameters tempParameters;
                var parametersPtr = new IntPtr(&tempParameters);
                Marshal.StructureToPtr(parameters, parametersPtr, false);

                var code = PaBinding.Pa_OpenStream(
                    new IntPtr(&stream),
                    IntPtr.Zero,
                    parametersPtr,
                    sampleRate,
                    FramesPerBuffer,
                    StreamFlags,
                    null,
                    IntPtr.Zero
                );

                PaBinding.Pa_MaybeThrow(code);
            }

            this.stream = stream;

            PaBinding.Pa_MaybeThrow(PaBinding.Pa_StartStream(stream));
        }

        public void Send(Span<float> samples) {
            unsafe {
                fixed (float* buffer = samples) {
                    var frames = samples.Length / channels;
                    PaBinding.Pa_WriteStream(stream, (IntPtr)buffer, frames);
                }
            }
        }

        public void Dispose() {
            if (disposed || stream == IntPtr.Zero) {
                return;
            }
            PaBinding.Pa_AbortStream(stream);
            PaBinding.Pa_CloseStream(stream);
            disposed = true;
        }
    }
}
