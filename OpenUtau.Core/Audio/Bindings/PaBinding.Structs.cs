using System;
using System.Runtime.InteropServices;

namespace OpenUtau.Audio.Bindings {
    internal static partial class PaBinding {
        [StructLayout(LayoutKind.Sequential)]
        public readonly struct PaVersionInfo {
            public readonly int versionMajor;
            public readonly int versionMinor;
            public readonly int versionSubMinor;

            [MarshalAs(UnmanagedType.LPStr)]
            public readonly string versionControlRevision;

            [MarshalAs(UnmanagedType.LPStr)]
            public readonly string verionText;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PaStreamParameters {
            public int device;
            public int channelCount;
            public PaSampleFormat sampleFormat;
            public double suggestedLatency;
            public IntPtr hostApiSpecificStreamInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public readonly struct PaDeviceInfo {
            public readonly int structVersion;

            [MarshalAs(UnmanagedType.LPUTF8Str)]
            public readonly string name;

            public readonly int hostApi;
            public readonly int maxInputChannels;
            public readonly int maxOutputChannels;
            public readonly double defaultLowInputLatency;
            public readonly double defaultLowOutputLatency;
            public readonly double defaultHighInputLatency;
            public readonly double defaultHighOutputLatency;
            public readonly double defaultSampleRate;
        }

        [StructLayout(LayoutKind.Sequential)]
        public readonly struct PaHostApiInfo {
            public readonly int structVersion;

            public readonly int type;

            [MarshalAs(UnmanagedType.LPStr)]
            public readonly string name;

            public readonly int deviceCount;
            public readonly int defaultInputDevice;
            public readonly int defaultOutputDevice;
        }
    }
}
