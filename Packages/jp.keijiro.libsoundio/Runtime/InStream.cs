// libsoundio C# thin wrapper class library
// https://github.com/keijiro/jp.keijiro.libsoundio

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace SoundIO
{
    // SoundIoInStream struct representation (used in read-callback)
    [StructLayout(LayoutKind.Sequential)]
    public struct InStreamData
    {
        #region Struct data members

        internal IntPtr device;
        internal Format format;
        internal int sampleRate;
        internal ChannelLayout layout;
        internal double softwareLatency;
        internal IntPtr userData;
        internal IntPtr readCallback;
        internal IntPtr overflowCallback;
        internal IntPtr errorCallback;
        internal IntPtr name;
        internal byte nonTerminalHint;
        internal int bytesPerFrame;
        internal int bytesPerSample;
        internal Error layoutError;

        #endregion

        #region Struct member accessors

        public Format Format => format;
        public int SampleRate => sampleRate;
        public ChannelLayout Layout => layout;
        public double SoftwareLatency => softwareLatency;
        public IntPtr UserData => userData;
        public string Name => Marshal.PtrToStringAnsi(name);
        public bool NonTerminalHint => nonTerminalHint != 0;
        public int BytesPerFrame => bytesPerFrame;
        public int BytesPerSample => bytesPerSample;

        #endregion

        #region Data reader methods

        public unsafe Error BeginRead(out ChannelArea* areas, ref int frameCount)
        {
            return _BeginRead(ref this, out areas, ref frameCount);
        }

        public Error EndRead()
        {
            return _EndRead(ref this);
        }

        #endregion

        #region Unmanaged functions

        [DllImport(Config.DllName, EntryPoint="soundio_instream_begin_read")]
        unsafe extern static Error _BeginRead
            (ref InStreamData stream, out ChannelArea* areas, ref int frameCount);

        [DllImport(Config.DllName, EntryPoint="soundio_instream_end_read")]
        extern static Error _EndRead(ref InStreamData stream);

        #endregion
    }

    // SoundIoInStream struct wrapper class
    public class InStream : SafeHandleZeroOrMinusOneIsInvalid
    {
        #region SafeHandle implementation

        InStream() : base(true) {}

        protected override bool ReleaseHandle()
        {
            _Destroy(this.handle);
            return true;
        }

        unsafe ref InStreamData Data => ref Unsafe.AsRef<InStreamData>((void*)handle);

        #endregion

        #region Struct member accessors

        public Format Format
        {
            get => Data.format;
            set => Data.format = value;
        }

        public int SampleRate
        {
            get => Data.sampleRate;
            set => Data.sampleRate = value;
        }

        public ChannelLayout Layout
        {
            get => Data.layout;
            set => Data.layout = value;
        }

        public double SoftwareLatency
        {
            get => Data.softwareLatency;
            set => Data.softwareLatency = value;
        }

        public IntPtr UserData
        {
            get => Data.userData;
            set => Data.userData = value;
        }

        public delegate void ReadCallbackDelegate(ref InStreamData stream, int frameCountMin, int frameCountMax);

        public ReadCallbackDelegate ReadCallback
        {
            get => Marshal.GetDelegateForFunctionPointer<ReadCallbackDelegate>(Data.readCallback);
            set => Data.readCallback = Marshal.GetFunctionPointerForDelegate(value);
        }

        public delegate void OverflowCallbackDelegate(ref InStreamData stream);

        public OverflowCallbackDelegate OverflowCallback
        {
            get => Marshal.GetDelegateForFunctionPointer<OverflowCallbackDelegate>(Data.overflowCallback);
            set => Data.overflowCallback = Marshal.GetFunctionPointerForDelegate(value);
        }

        public delegate void ErrorCallbackDelegate(ref InStreamData stream, Error error);

        public ErrorCallbackDelegate ErrorCallback
        {
            get => Marshal.GetDelegateForFunctionPointer<ErrorCallbackDelegate>(Data.errorCallback);
            set => Data.errorCallback = Marshal.GetFunctionPointerForDelegate(value);
        }

        public string Name => Marshal.PtrToStringAnsi(Data.name);

        public bool NonTerminalHint
        {
            get => Data.nonTerminalHint != 0;
            set => Data.nonTerminalHint = value ? (byte)1 : (byte)0;
        }

        public int BytesPerFrame => Data.bytesPerFrame;
        public int BytesPerSample => Data.bytesPerSample;

        public Error LayoutError => Data.layoutError;

        #endregion

        #region Public properties and methods

        static public InStream Create(Device device) => _Create(device);

        public Error Open() => _Open(this);
        public Error Start() => _Start(this);
        public Error Pause(bool pause) => _Pause(this, pause ? (byte)1 : (byte)0);
        public Error GetLatency(out double latency) => _GetLatency(this, out latency);

        #endregion

        #region Unmanaged functions

        [DllImport(Config.DllName, EntryPoint="soundio_instream_destroy")]
        extern static void _Destroy(IntPtr stream);

        [DllImport(Config.DllName, EntryPoint="soundio_instream_create")]
        extern static InStream _Create(Device device);

        [DllImport(Config.DllName, EntryPoint="soundio_instream_open")]
        extern static Error _Open(InStream stream);

        [DllImport(Config.DllName, EntryPoint="soundio_instream_start")]
        extern static Error _Start(InStream stream);

        [DllImport(Config.DllName, EntryPoint="soundio_instream_pause")]
        extern static Error _Pause(InStream stream, byte pause);

        [DllImport(Config.DllName, EntryPoint="soundio_instream_get_latency")]
        extern static Error _GetLatency(InStream stream, out double latency);

        #endregion
    }
}
