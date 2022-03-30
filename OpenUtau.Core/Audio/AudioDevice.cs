using OpenUtau.Audio.Bindings;

namespace OpenUtau.Audio {
    readonly struct AudioDevice {
        public int DeviceIndex { get; }
        public string Name { get; }
        public string HostApi { get; }
        public int MaxInputChannels { get; }
        public int MaxOutputChannels { get; }
        public double DefaultLowInputLatency { get; }
        public double DefaultLowOutputLatency { get; }
        public double DefaultHighInputLatency { get; }
        public double DefaultHighOutputLatency { get; }
        public int DefaultSampleRate { get; }
        public AudioDevice(PaBinding.PaDeviceInfo device, int deviceIndex) {
            var hostApi = PaBinding.GetHostApiInfo(device.hostApi);
            DeviceIndex = deviceIndex;
            Name = device.name;
            HostApi = hostApi.name;
            MaxInputChannels = device.maxInputChannels;
            MaxOutputChannels = device.maxOutputChannels;
            DefaultLowInputLatency = device.defaultLowInputLatency;
            DefaultLowOutputLatency = device.defaultLowOutputLatency;
            DefaultHighInputLatency = device.defaultHighInputLatency;
            DefaultHighOutputLatency = device.defaultHighOutputLatency;
            DefaultSampleRate = (int)device.defaultSampleRate;
        }
    }
}
