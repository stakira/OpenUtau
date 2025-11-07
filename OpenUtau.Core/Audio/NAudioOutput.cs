using System;
using System.Collections.Generic;
using NAudio.Wave;
using OpenUtau.Core.Util;

namespace OpenUtau.Audio {
#if !WINDOWS
    public class NAudioOutput : DummyAudioOutput { }
#else
    public class NAudioOutput : IAudioOutput {
        const int Channels = 2;

        private readonly object lockObj = new object();
        private WaveOutEvent waveOutEvent;
        private int deviceNumber;

        public NAudioOutput() {
            if (Guid.TryParse(Preferences.Default.PlaybackDevice, out var guid)) {
                SelectDevice(guid, Preferences.Default.PlaybackDeviceNumber);
            } else {
                SelectDevice(new Guid(), 0);
            }
        }

        public PlaybackState PlaybackState {
            get {
                lock (lockObj) {
                    return waveOutEvent == null ? PlaybackState.Stopped : waveOutEvent.PlaybackState;
                }
            }
        }

        public int DeviceNumber => deviceNumber;

        public long GetPosition() {
            lock (lockObj) {
                return waveOutEvent == null
                    ? 0
                    : waveOutEvent.GetPosition() / Channels;
            }
        }

        public void Init(ISampleProvider sampleProvider) {
            lock (lockObj) {
                if (waveOutEvent != null) {
                    waveOutEvent.Stop();
                    waveOutEvent.Dispose();
                }
                waveOutEvent = new WaveOutEvent() {
                    DeviceNumber = deviceNumber,
                };
                waveOutEvent.Init(sampleProvider);
            }
        }

        public void Pause() {
            lock (lockObj) {
                if (waveOutEvent != null) {
                    waveOutEvent.Pause();
                }
            }
        }

        public void Play() {
            lock (lockObj) {
                if (waveOutEvent != null) {
                    waveOutEvent.Play();
                }
            }
        }

        public void Stop() {
            lock (lockObj) {
                if (waveOutEvent != null) {
                    waveOutEvent.Stop();
                    waveOutEvent.Dispose();
                    waveOutEvent = null;
                }
            }
        }

        public void SelectDevice(Guid guid, int deviceNumber) {
            Preferences.Default.PlaybackDevice = guid.ToString();
            Preferences.Default.PlaybackDeviceNumber = deviceNumber;
            Preferences.Save();
            // Product guid may not be unique. Use device number first.
            if (deviceNumber < WaveOut.DeviceCount && WaveOut.GetCapabilities(deviceNumber).ProductGuid == guid) {
                this.deviceNumber = deviceNumber;
                return;
            }
            // If guid does not match, device number may have changed. Search guid instead.
            this.deviceNumber = 0;
            for (int i = 0; i < WaveOut.DeviceCount; ++i) {
                if (WaveOut.GetCapabilities(i).ProductGuid == guid) {
                    this.deviceNumber = i;
                    break;
                }
            }
        }

        public List<AudioOutputDevice> GetOutputDevices() {
            var outDevices = new List<AudioOutputDevice>();
            for (int i = 0; i < WaveOut.DeviceCount; ++i) {
                var capability = WaveOut.GetCapabilities(i);
                outDevices.Add(new AudioOutputDevice {
                    api = "WaveOut",
                    name = capability.ProductName,
                    deviceNumber = i,
                    guid = capability.ProductGuid,
                });
            }
            return outDevices;
        }
    }
#endif
}
