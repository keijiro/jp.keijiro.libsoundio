// libsoundio C# thin wrapper class library
// https://github.com/keijiro/jp.keijiro.libsoundio

namespace SoundIO
{
    static class Unsafe
    {
        public unsafe static ref T AsRef<T>(void* p) where T : struct
#if USE_UNITY_UNSAFE_UTILITY
          => ref Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AsRef<T>(p);
#else
          => ref System.Runtime.CompilerServices.Unsafe.AsRef<T>(p);
#endif

    }
}
