namespace OpenUtau.Audio.Bindings {
    internal static partial class PaBinding {
        public enum PaSampleFormat : long {
            paFloat32 = 0x00000001,
            paInt32 = 0x00000002,
            paInt24 = 0x00000004,
            paInt16 = 0x00000008,
            paInt8 = 0x00000010,
            paUInt8 = 0x00000020,
            paCustomFormat = 0x00010000,
            paNonInterleaved = 0x80000000,
        }

        public enum PaStreamCallbackFlags : long {
            paInputUnderflow = 0x00000001,
            paInputOverflow = 0x00000002,
            paOutputUnderflow = 0x00000004,
            paOutputOverflow = 0x00000008,
            paPrimingOutput = 0x00000010
        }

        public enum PaStreamCallbackResult {
            paContinue = 0,
            paComplete = 1,
            paAbort = 2
        }

        public enum PaStreamFlags : long {
            paNoFlag = 0,
            paClipOff = 0x00000001,
            paDitherOff = 0x00000002,
            paPrimeOutputBuffersUsingStreamCallback = 0x00000008,
            paPlatformSpecificFlags = 0xFFFF0000
        }
    }
}
