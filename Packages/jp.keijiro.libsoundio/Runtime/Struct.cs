using System;
using System.Runtime.InteropServices;

namespace SoundIO
{
    //
    // Basic structs used in libsoundio API
    //

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
