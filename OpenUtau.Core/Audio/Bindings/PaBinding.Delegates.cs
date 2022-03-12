using System;
using System.Runtime.InteropServices;

namespace OpenUtau.Audio.Bindings {
    internal static partial class PaBinding {
        private const string dllName = "portaudio";

        public delegate PaStreamCallbackResult PaStreamCallback(
            IntPtr input,
            IntPtr output,
            long frameCount,
            IntPtr timeInfo,
            PaStreamCallbackFlags statusFlags,
            IntPtr userData
        );

        [DllImport(dllName)] public static extern int Pa_Initialize();
        [DllImport(dllName)] public static extern int Pa_Terminate();
        [DllImport(dllName)] public static extern IntPtr Pa_GetVersionInfo();
        [DllImport(dllName)] public static extern IntPtr Pa_GetErrorText(int code);
        [DllImport(dllName)] public static extern int Pa_GetDefaultOutputDevice();
        [DllImport(dllName)] public static extern IntPtr Pa_GetDeviceInfo(int device);
        [DllImport(dllName)] public static extern int Pa_GetDeviceCount();
        [DllImport(dllName)] public static extern int Pa_GetDefaultHostApi();
        [DllImport(dllName)] public static extern IntPtr Pa_GetHostApiInfo(int device);
        [DllImport(dllName)] public static extern int Pa_GetHostApiCount();

        [DllImport(dllName)]
        public static extern int Pa_IsFormatSupported(
            IntPtr inputParameters,
            IntPtr outputParameters,
            double sampleRate);

        [DllImport(dllName)]
        public static extern int Pa_OpenStream(
            IntPtr stream,
            IntPtr inputParameters,
            IntPtr outputParameters,
            double sampleRate,
            long framesPerBuffer,
            PaStreamFlags streamFlags,
            PaStreamCallback streamCallback,
            IntPtr userData);

        [DllImport(dllName)] public static extern int Pa_StartStream(IntPtr stream);
        [DllImport(dllName)] public static extern int Pa_WriteStream(IntPtr stream, IntPtr buffer, long frames);
        [DllImport(dllName)] public static extern int Pa_ReadStream(IntPtr stream, IntPtr buffer, long frames);
        [DllImport(dllName)] public static extern int Pa_AbortStream(IntPtr stream);
        [DllImport(dllName)] public static extern int Pa_CloseStream(IntPtr stream);
    }
}
