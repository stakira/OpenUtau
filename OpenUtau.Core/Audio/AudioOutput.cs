using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using OpenUtau.Audio.Bindings;
using OpenUtau.Core;
using OpenUtau.Core.Util;
using Serilog;

namespace OpenUtau.Audio {
    public class AudioOutput : IAudioOutput, IDisposable {
        public const int Channels = 2;

        public PlaybackState PlaybackState { get; private set; }
        public int DeviceNumber { get; private set; }

        private readonly object lockObj = new object();
        private ConcurrentQueue<AudioFrame> queue = new ConcurrentQueue<AudioFrame>();
        private AudioEngine audioEngine;
        private ISampleProvider sampleProvider;
        private float[] buffer;
        private double bufferedTimeMs;
        private double currentTimeMs;
        private Thread pushThread;
        private Thread pullThread;
        private bool eof;
        private bool shutdown;
        private bool disposed;

        public AudioOutput() {
            var path = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "libs");
            PaBinding.InitializeBindings(new LibraryLoader(path, "portaudio"));
            PaBinding.Pa_Initialize();

            buffer = new float[0];
            try {
                if (Preferences.Default.PlaybackDeviceIndex != null) {
                    try {
                        SelectDevice(new Guid(), Preferences.Default.PlaybackDeviceIndex.Value);
                    } catch {
                        SelectDevice(new Guid(), PaBinding.Pa_GetDefaultOutputDevice());
                        throw;
                    }
                } else {
                    SelectDevice(new Guid(), PaBinding.Pa_GetDefaultOutputDevice());
                }
            } catch (Exception e) {
                Log.Error(e, "Failed to initialize audio device");
            }

            pullThread = new Thread(Pull) { IsBackground = true };
            pushThread = new Thread(Push) { IsBackground = true };
            pullThread.Start();
            pushThread.Start();
        }

        public List<AudioOutputDevice> GetOutputDevices() {
            List<AudioOutputDevice> devices = new List<AudioOutputDevice>();
            int count = PaBinding.Pa_GetDeviceCount();
            PaBinding.Pa_MaybeThrow(count);
            for (int i = 0; i < count; ++i) {
                var device = GetEligibleOutputDevice(i);
                if (device is AudioDevice dev) {
                    devices.Add(new AudioOutputDevice() {
                        api = dev.HostApi,
                        name = dev.Name,
                        deviceNumber = dev.DeviceIndex,
                        guid = new Guid(),
                    });
                }
            }
            return devices;
        }

        public long GetPosition() {
            var latency = audioEngine?.latency ?? 0;
            var sampleRate = audioEngine?.sampleRate ?? 44100;
            return (long)(Math.Max(0, currentTimeMs - latency) / 1000 * sampleRate * 2 /* currently assumes 16 bit */ * Channels);
        }

        public void Init(ISampleProvider sampleProvider) {
            PlaybackState = PlaybackState.Stopped;
            eof = false;
            queue.Clear();
            bufferedTimeMs = 0;
            currentTimeMs = 0;
            var sampleRate = audioEngine?.sampleRate ?? 44100;
            if (sampleRate != sampleProvider.WaveFormat.SampleRate) {
                sampleProvider = new WdlResamplingSampleProvider(sampleProvider, sampleRate);
            }
            this.sampleProvider = sampleProvider.ToStereo();
        }

        public void Pause() {
            PlaybackState = PlaybackState.Paused;
        }

        public void Play() {
            eof = false;
            queue.Clear();
            currentTimeMs = 0;
            PlaybackState = PlaybackState.Playing;
        }

        public void SelectDevice(Guid guid, int deviceNumber) {
            lock (lockObj) {
                if (audioEngine == null || audioEngine.device.DeviceIndex != deviceNumber) {
                    var device = GetEligibleOutputDevice(deviceNumber);
                    if (device is AudioDevice dev) {
                        audioEngine?.Dispose();
                        audioEngine = new AudioEngine(
                            dev,
                            Channels,
                            dev.DefaultSampleRate,
                            dev.DefaultHighOutputLatency);
                        DeviceNumber = deviceNumber;
                        buffer = new float[dev.DefaultSampleRate * Channels * 10 / 1000]; // 10ms at 44.1kHz
                    }
                    Preferences.Default.PlaybackDeviceIndex = DeviceNumber;
                    Preferences.Save();
                }
            }
        }

        private AudioDevice? GetEligibleOutputDevice(int index) {
            var device = new AudioDevice(PaBinding.Pa_GetDeviceInfo(index), index);
            if (device.MaxOutputChannels < Channels) {
                return null;
            }
            var api = device.HostApi.ToLowerInvariant();
            if (api.Contains("wasapi") || api.Contains("wdm-ks")) {
                return null;
            }
            var parameters = new PaBinding.PaStreamParameters {
                channelCount = Channels,
                device = device.DeviceIndex,
                hostApiSpecificStreamInfo = IntPtr.Zero,
                sampleFormat = PaBinding.PaSampleFormat.paFloat32,
            };
            unsafe {
                int code = PaBinding.Pa_IsFormatSupported(IntPtr.Zero, new IntPtr(&parameters), 44100);
                if (code < 0) {
                    return null;
                }
            }
            return device;
        }

        public void Stop() {
            PlaybackState = PlaybackState.Stopped;
            bufferedTimeMs = 0;
            currentTimeMs = 0;
            sampleProvider = null;
            queue.Clear();
        }

        private void Push() {
            while (!shutdown) {
                if (PlaybackState == PlaybackState.Paused ||
                    PlaybackState == PlaybackState.Stopped) {
                    Thread.Sleep(10);
                    continue;
                }

                AudioEngine engine = audioEngine;
                if (engine == null) {
                    Thread.Sleep(10);
                    continue;
                }

                if (queue.Count == 0) {
                    if (eof) {
                        PlaybackState = PlaybackState.Stopped;
                        Thread.Sleep(10);
                        continue;
                    }
                    Thread.Sleep(10);
                    continue;
                }

                if (!queue.TryDequeue(out var frame)) {
                    Thread.Sleep(10);
                    continue;
                }

                if (PlaybackState != PlaybackState.Playing) {
                    PlaybackState = PlaybackState.Playing;
                }
                engine.Send(frame.Data);
                currentTimeMs = frame.PresentationTime;
            }
        }

        private void Pull() {
            while (!shutdown) {
                var sp = sampleProvider;
                if (sp == null) {
                    Thread.Sleep(10);
                    continue;
                }
                if (PlaybackState == PlaybackState.Paused ||
                    PlaybackState == PlaybackState.Stopped) {
                    Thread.Sleep(10);
                    continue;
                }
                if (queue.Count >= 10) {
                    Thread.Sleep(10);
                    continue;
                }

                var n = sp.Read(buffer, 0, buffer.Length);
                if (n == 0) {
                    eof = true;
                    Thread.Sleep(10);
                    continue;
                }
                var data = new float[n];
                Array.Copy(buffer, data, n);
                var frame = new AudioFrame(bufferedTimeMs, data);
                queue.Enqueue(frame);
                var sampleRate = audioEngine?.sampleRate ?? 44100;
                bufferedTimeMs += n * 1000.0 / sampleRate / Channels;
            }
        }

        public void Dispose() {
            if (disposed) {
                return;
            }

            PlaybackState = PlaybackState.Stopped;
            shutdown = true;

            if (pushThread != null) {
                while (pushThread.IsAlive) {
                    Thread.Sleep(10);
                }
                pushThread = null;
            }
            if (pullThread != null) {
                while (pullThread.IsAlive) {
                    Thread.Sleep(10);
                }
                pullThread = null;
            }
            queue.Clear();

            GC.SuppressFinalize(this);

            disposed = true;
        }
    }
}
