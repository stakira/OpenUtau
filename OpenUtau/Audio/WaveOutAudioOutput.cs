using System;
using System.Collections.Generic;
using NAudio.Wave;
using OpenUtau.Core;
using OpenUtau.Core.Util;

namespace OpenUtau.Audio {
    class WaveOutAudioOutput : IAudioOutput {
        private object lockObj = new object();
        private WaveOut waveOut;
        private int deviceNumber;

        public WaveOutAudioOutput() {
            if (Guid.TryParse(Preferences.Default.PlaybackDevice, out var guid)) {
                SelectDevice(guid, Preferences.Default.PlaybackDeviceNumber);
            } else {
                SelectDevice(new Guid(), 0);
            }
        }

        public PlaybackState PlaybackState {
            get {
                lock (lockObj) {
                    return waveOut == null ? PlaybackState.Stopped : waveOut.PlaybackState;
                }
            }
        }

        public int DeviceNumber => deviceNumber;

        public long GetPosition() {
            lock (lockObj) {
                return waveOut == null ? 0 : waveOut.GetPosition();
            }
        }

        public void Init(ISampleProvider sampleProvider) {
            lock (lockObj) {
                if (waveOut != null) {
                    waveOut.Stop();
                    waveOut.Dispose();
                }
                waveOut = new WaveOut() {
                    DeviceNumber = deviceNumber,
                };
                waveOut.Init(sampleProvider);
            }
        }

        public void Pause() {
            lock (lockObj) {
                if (waveOut != null) {
                    waveOut.Pause();
                }
            }
        }

        public void Play() {
            lock (lockObj) {
                if (waveOut != null) {
                    waveOut.Play();
                }
            }
        }

        public void Stop() {
            lock (lockObj) {
                if (waveOut != null) {
                    waveOut.Stop();
                    waveOut.Dispose();
                    waveOut = null;
                }
            }
        }

        public void SelectDevice(Guid productGuid, int deviceNumber) {
            // Product guid may not be unique. Use device number first.
            if (deviceNumber < WaveOut.DeviceCount && WaveOut.GetCapabilities(deviceNumber).ProductGuid == productGuid) {
                this.deviceNumber = deviceNumber;
                return;
            }
            // If guid does not match, device number may have changed. Search guid instead.
            this.deviceNumber = 0;
            for (int i = 0; i < WaveOut.DeviceCount; ++i) {
                if (WaveOut.GetCapabilities(i).ProductGuid == productGuid) {
                    this.deviceNumber = i;
                    break;
                }
            }
        }

        public List<WaveOutCapabilities> GetOutputDevices() {
            var outDevices = new List<WaveOutCapabilities>();
            for (int i = 0; i < WaveOut.DeviceCount; ++i) {
                outDevices.Add(WaveOut.GetCapabilities(i));
            }
            return outDevices;
        }
    }
}
