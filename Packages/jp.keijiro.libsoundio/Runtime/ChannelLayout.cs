using System;
using System.Runtime.InteropServices;

namespace SoundIO
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ChannelLayout
    {
        #region Struct data

        IntPtr _name;
        int _channelCount;
        unsafe fixed int _channels[MaxChannels];

        #endregion

        #region Public properties

        public const int MaxChannels = 24;

        public string Name => Marshal.PtrToStringAnsi(_name);
        public int ChannelCount => _channelCount;

        unsafe public ReadOnlySpan<Channel> Channels { get {
            fixed (int* p = _channels) return new ReadOnlySpan<Channel>(p, ChannelCount);
        } }

        #endregion

        #region Builtin layouts

        public enum Builtin
        {
            Mono,
            Stereo,
            _2Point1,
            _3Point0,
            _3Point0Back,
            _3Point1,
            _4Point0,
            Quad,
            QuadSide,
            _4Point1,
            _5Point0Back,
            _5Point0Side,
            _5Point1,
            _5Point1Back,
            _6Point0Side,
            _6Point0Front,
            Hexagonal,
            _6Point1,
            _6Point1Back,
            _6Point1Front,
            _7Point0,
            _7Point0Front,
            _7Point1,
            _7Point1Wide,
            _7Point1WideBack,
            Octagonal
        }

        static public ChannelLayout GetBuiltin(Builtin id) => _GetBuiltin(id);

        #endregion

        #region Native methods

        [DllImport("SoundIO.dll", EntryPoint="soundio_channel_layout_builtin_count")]
        extern static int _BuiltinCount();

        [DllImport("SoundIO.dll", EntryPoint="soundio_channel_layout_get_builtin")]
        extern static ref readonly ChannelLayout _GetBuiltin(Builtin id);

        #endregion
    }
}
