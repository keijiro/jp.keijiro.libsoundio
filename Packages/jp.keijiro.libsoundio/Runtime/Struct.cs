// libsoundio C# thin wrapper class library
// https://github.com/keijiro/jp.keijiro.libsoundio

using System;
using System.Runtime.InteropServices;

namespace SoundIO
{
    // Basic structs defined in libsoundio

    [StructLayout(LayoutKind.Sequential)]
    public struct SampleRateRange
    {
        public int Min;
        public int Max;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ChannelArea
    {
        public IntPtr Pointer;
        public int Step;
    }
}
