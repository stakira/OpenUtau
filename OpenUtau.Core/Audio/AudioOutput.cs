using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using NAudio.Wave;
using OpenUtau.Audio.Bindings;
using OpenUtau.Core;
using OpenUtau.Core.Util;

namespace OpenUtau.Audio {
    public class AudioOutput : IAudioOutput, IDisposable {
        public PlaybackState PlaybackState { get; private set; }
        public int DeviceNumber { get; private set; }

        private readonly object lockObj = new object();
        private ConcurrentQueue<AudioFrame> queue = new ConcurrentQueue<AudioFrame>();
        private AudioEngine audioEngine;
        private ISampleProvider sampleProvider;
        private float[] buffer;
        private double outputLatency;
        private double bufferedTimeMs;
        private double currentTimeMs;
        private Thread pushThread;
        private Thread pullThread;
        private bool eof;
        private bool shutdown;
        private bool disposed;

        public AudioOutput() {
            var path = Path.Combine(Environment.CurrentDirectory, "libs");
            PaBinding.InitializeBindings(new LibraryLoader(path, "portaudio"));
            PaBinding.Pa_Initialize();

            buffer = new float[44100 * 1 * 10 / 1000]; // mono 10ms
            if (Guid.TryParse(Preferences.Default.PlaybackDevice, out var guid)) {
                SelectDevice(guid, Preferences.Default.PlaybackDeviceNumber);
            } else {
                SelectDevice(new Guid(), PaBinding.Pa_GetDefaultOutputDevice());
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
                var device = new AudioDevice(PaBinding.Pa_GetDeviceInfo(i), i);
                if (device.MaxOutputChannels > 0) {
                    devices.Add(new AudioOutputDevice() {
                        api = device.HostApi,
                        name = device.Name,
                        deviceNumber = device.DeviceIndex,
                        guid = new Guid(),
                    });
                }
            }
            return devices;
        }

        public long GetPosition() {
            return (long)(Math.Max(0, currentTimeMs - outputLatency) / 1000 * 44100 * 4);
        }

        public void Init(ISampleProvider sampleProvider) {
            PlaybackState = PlaybackState.Stopped;
            eof = false;
            queue.Clear();
            bufferedTimeMs = 0;
            currentTimeMs = 0;
            this.sampleProvider = sampleProvider;
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
                deviceNumber = PaBinding.Pa_GetDefaultOutputDevice(); // Always use default device for now.
                if (audioEngine == null || audioEngine.device.DeviceIndex != deviceNumber) {
                    audioEngine?.Dispose();
                    var device = new AudioDevice(PaBinding.Pa_GetDeviceInfo(deviceNumber), deviceNumber);
                    outputLatency = device.DefaultHighOutputLatency;
                    audioEngine = new AudioEngine(device, 1, 44100, outputLatency);
                    DeviceNumber = deviceNumber;
                }
            }
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
                    continue;
                }

                if (queue.Count == 0) {
                    if (eof) {
                        PlaybackState = PlaybackState.Stopped;
                        continue;
                    }
                    Thread.Sleep(10);
                    continue;
                }

                if (!queue.TryDequeue(out var frame)) {
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
                    continue;
                }
                var data = new float[n];
                Array.Copy(buffer, data, n);
                var frame = new AudioFrame(bufferedTimeMs, data);
                queue.Enqueue(frame);
                bufferedTimeMs += 10;
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
