using System;
using System.Runtime.InteropServices;

namespace OpenUtau.Audio.Bindings {
    internal static partial class PaBinding {
        public static void InitializeBindings(LibraryLoader loader) {
            _initialize = loader.LoadFunc<Initialize>(nameof(Pa_Initialize));
            _terminate = loader.LoadFunc<Terminate>(nameof(Pa_Terminate));

            _getVersionInfo = loader.LoadFunc<GetVersionInfo>(nameof(Pa_GetVersionInfo));
            _getErrorText = loader.LoadFunc<GetErrorText>(nameof(Pa_GetErrorText));

            _getDefaultOutputDevice = loader.LoadFunc<GetDefaultOutputDevice>(nameof(Pa_GetDefaultOutputDevice));
            _getDeviceInfo = loader.LoadFunc<GetDeviceInfo>(nameof(Pa_GetDeviceInfo));
            _getDeviceCount = loader.LoadFunc<GetDeviceCount>(nameof(Pa_GetDeviceCount));

            _getDefaultHostApi = loader.LoadFunc<GetDefaultHostApi>(nameof(Pa_GetDefaultHostApi));
            _getHostApiInfo = loader.LoadFunc<GetHostApiInfo>(nameof(Pa_GetHostApiInfo));
            _getHostApiCount = loader.LoadFunc<GetHostApiCount>(nameof(Pa_GetHostApiCount));

            _openStream = loader.LoadFunc<OpenStream>(nameof(Pa_OpenStream));
            _readStream = loader.LoadFunc<ReadStream>(nameof(Pa_ReadStream));
            _writeStream = loader.LoadFunc<WriteStream>(nameof(Pa_WriteStream));
            _startStream = loader.LoadFunc<StartStream>(nameof(Pa_StartStream));
            _abortStream = loader.LoadFunc<AbortStream>(nameof(Pa_AbortStream));
            _closeStream = loader.LoadFunc<CloseStream>(nameof(Pa_CloseStream));
        }

        public static int Pa_Initialize() => _initialize();
        public static int Pa_Terminate() => _terminate();
        public static IntPtr Pa_GetVersionInfo() => _getVersionInfo();
        public static string Pa_GetErrorText(int code) => Marshal.PtrToStringAnsi(_getErrorText(code));

        public static void Pa_MaybeThrow(int code) {
            if (code >= 0) {
                return;
            }
            throw new Exception(Marshal.PtrToStringAnsi(_getErrorText(code)));
        }

        public static int Pa_GetDefaultOutputDevice() => _getDefaultOutputDevice();
        public static PaDeviceInfo Pa_GetDeviceInfo(int device) => Marshal.PtrToStructure<PaDeviceInfo>(_getDeviceInfo(device));
        public static int Pa_GetDeviceCount() => _getDeviceCount();
        public static PaHostApiInfo Pa_GetHostApiInfo(int hostApi) => Marshal.PtrToStructure<PaHostApiInfo>(_getHostApiInfo(hostApi));
        public static int Pa_GetDefaultHostApi() => _getDefaultHostApi();
        public static int Pa_GetHostApiCount() => _getHostApiCount();

        public static int Pa_OpenStream(
            IntPtr stream,
            IntPtr inputParameters,
            IntPtr outputParameters,
            double sampleRate,
            long framesPerBuffer,
            PaStreamFlags streamFlags,
            PaStreamCallback streamCallback,
            IntPtr userData) {
            return _openStream(
                stream,
                inputParameters,
                outputParameters,
                sampleRate,
                framesPerBuffer,
                streamFlags,
                streamCallback,
                userData
            );
        }

        public static int Pa_StartStream(IntPtr stream) => _startStream(stream);
        public static int Pa_ReadStream(IntPtr stream, IntPtr buffer, long frames) => _readStream(stream, buffer, frames);
        public static int Pa_WriteStream(IntPtr stream, IntPtr buffer, long frames) => _writeStream(stream, buffer, frames);
        public static int Pa_AbortStream(IntPtr stream) => _abortStream(stream);
        public static int Pa_CloseStream(IntPtr stream) => _closeStream(stream);
    }
}
