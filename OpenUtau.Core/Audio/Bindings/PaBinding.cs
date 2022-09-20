using System;
using System.Runtime.InteropServices;

namespace OpenUtau.Audio.Bindings {
    internal static partial class PaBinding {
        public static string GetErrorText(int code) => Marshal.PtrToStringAnsi(Pa_GetErrorText(code));

        public static void MaybeThrow(int code) {
            if (code >= 0) {
                return;
            }
            throw new Exception(Marshal.PtrToStringAnsi(Pa_GetErrorText(code)));
        }

        public static PaDeviceInfo GetDeviceInfo(int device) => Marshal.PtrToStructure<PaDeviceInfo>(Pa_GetDeviceInfo(device));
        public static PaHostApiInfo GetHostApiInfo(int hostApi) => Marshal.PtrToStructure<PaHostApiInfo>(Pa_GetHostApiInfo(hostApi));
    }
}
