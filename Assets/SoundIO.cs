using System;
using System.Runtime.InteropServices;
using SafeHandleZeroOrMinusOneIsInvalid = Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid;
using Unsafe = System.Runtime.CompilerServices.Unsafe;

namespace SoundIO
{
    public enum Channel
    {
        Invalid,

        FrontLeft, FrontRight, FrontCenter,
        Lfe,
        BackLeft, BackRight,
        FrontLeftCenter, FrontRightCenter,
        BackCenter,
        SideLeft, SideRight,
        TopCenter,
        TopFrontLeft, TopFrontCenter, TopFrontRight,
        TopBackLeft, TopBackCenter, TopBackRight,

        BackLeftCenter, BackRightCenter,
        FrontLeftWide, FrontRightWide,
        FrontLeftHigh, FrontCenterHigh, FrontRightHigh,
        TopFrontLeftCenter, TopFrontRightCenter,
        TopSideLeft, TopSideRight,
        LeftLfe, RightLfe, Lfe2,
        BottomCenter, BottomLeftCenter, BottomRightCenter,

        MsMid, MsSide,

        AmbisonicW, AmbisonicX, AmbisonicY, AmbisonicZ,

        XyX, XyY,

        HeadphonesLeft, HeadphonesRight,
        ClickTrack,
        ForeignLanguage,
        HearingImpaired,
        Narration,
        Haptic,
        DialogCentricMix,

        Aux, Aux0, Aux1, Aux2, Aux3, Aux4, Aux5, Aux6, Aux7,
        Aux8, Aux9, Aux10, Aux11, Aux12, Aux13, Aux14, Aux15
    }

    public enum ChannelLayoutID
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

    public enum Format
    {
        Invalid,
        S8, U8,
        S16LE, S16BE, U16LE, U16BE,
        S24LE, S24BE, U24LE, U24BE,
        S32LE, S32BE, U32LE, U32BE,
        Float32LE, Float32BE, Float64LE, Float64BE,
    }

    public static class NativeMethods
    {
        public enum Error
        {
            None,
            NoMem,
            InitAudioBackend,
            SystemResources,
            OpeningDevice,
            NoSuchDevice,
            Invalid,
            BackendUnavailable,
            Streaming,
            IncompatibleDevice,
            NoSuchClient,
            IncompatibleBackend,
            BackendDisconnected,
            Underflow,
            EncodingString
        }

        public enum Backend
        {
            None,
            Jack,
            PulseAudio,
            Alsa,
            CoreAudio,
            Wasapi,
            Dummy
        }

        public enum DeviceAim { Input, Output };

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

        #region SoundIO Context

        [StructLayout(LayoutKind.Sequential)]
        public struct ContextData
        {
            public IntPtr Userdata;
            IntPtr _onDevicesChange;
            IntPtr _onBackendDisconnect;
            IntPtr _onEventsSignal;
            public Backend CurrentBackend;
            IntPtr _appName;

            public delegate void OnDevicesChangeCallback(ref ContextData context);
            public delegate void OnBackendDisconnectCallback(ref ContextData context, Error error);
            public delegate void OnEventsSignalCallback(ref ContextData context);

            public OnDevicesChangeCallback OnDevicesChange
            {
                get => Marshal.GetDelegateForFunctionPointer<OnDevicesChangeCallback>(_onDevicesChange);
                set => _onDevicesChange = Marshal.GetFunctionPointerForDelegate(new OnDevicesChangeCallback(value));
            }

            public OnBackendDisconnectCallback OnBackendDisconnect
            {
                get => Marshal.GetDelegateForFunctionPointer<OnBackendDisconnectCallback>(_onBackendDisconnect);
                set => _onBackendDisconnect = Marshal.GetFunctionPointerForDelegate(new OnBackendDisconnectCallback(value));
            }

            public OnEventsSignalCallback OnEventSignal
            {
                get => Marshal.GetDelegateForFunctionPointer<OnEventsSignalCallback>(_onEventsSignal);
                set => _onEventsSignal = Marshal.GetFunctionPointerForDelegate(new OnEventsSignalCallback(value));
            }

            public string AppName => Marshal.PtrToStringAnsi(_appName);
        }

        public class Context : SafeHandleZeroOrMinusOneIsInvalid
        {
            private Context() : base(true) {}

            protected override bool ReleaseHandle()
            {
                Destroy(this.handle);
                return true;
            }

            [DllImport("SoundIO.dll", EntryPoint="soundio_destroy")]
            extern static void Destroy(IntPtr context);
        }

        [DllImport("SoundIO.dll", EntryPoint="soundio_create")]
        public extern static Context Create();

        [DllImport("SoundIO.dll", EntryPoint="soundio_connect")]
        public extern static Error Connect(Context context);

        [DllImport("SoundIO.dll", EntryPoint="soundio_connect_backend")]
        public extern static Error Connect(Context context, Backend backend);

        [DllImport("SoundIO.dll", EntryPoint="soundio_disconnect")]
        public extern static void Disconnect(Context context);

        [DllImport("SoundIO.dll", EntryPoint="soundio_flush_events")]
        public extern static void FlushEvents(Context context);

        #endregion

        #region Channel layout

        [StructLayout(LayoutKind.Sequential)]
        unsafe public struct ChannelLayout
        {
            const int MaxChannels = 24;

            IntPtr _name;
            public int ChannelCount;
            fixed int _channels[MaxChannels];

            public string Name => Marshal.PtrToStringAnsi(_name);

            public ReadOnlySpan<Channel> Channels { get {
                fixed (int* p = _channels)
                    return new ReadOnlySpan<Channel>(p, ChannelCount);
            } }
        }

        [DllImport("SoundIO.dll", EntryPoint="soundio_channel_layout_builtin_count")]
        public extern static int ChannelLayoutBuiltinCount();

        [DllImport("SoundIO.dll", EntryPoint="soundio_channel_layout_get_builtin")]
        public extern static ref readonly ChannelLayout
            ChannelLayoutGetBuiltin(ChannelLayoutID id);

        #endregion

        #region Device operations

        [StructLayout(LayoutKind.Sequential)]
        public struct DeviceData
        {
            IntPtr _context;
            IntPtr _id;
            IntPtr _name;
            public DeviceAim Aim;

            IntPtr _layouts;
            int _layoutCount;
            public ChannelLayout CurrentLayout;

            IntPtr _formats;
            int _formatCount;
            public Format CurrentFormat;

            IntPtr _sampleRates;
            int _sampleRateCount;
            public int SampleRateCurent;

            public double SoftwareLatencyMin;
            public double SoftwareLatencyMax;
            public double SoftwareLatencyCurrent;

            byte _isRaw;
            int _refCount;
            Error _probeError;

            public string Name => Marshal.PtrToStringAnsi(_name);
            public string ID => Marshal.PtrToStringAnsi(_id);

            unsafe public Span<ChannelLayout> Layouts =>
                new Span<ChannelLayout>((void*)_layouts, _layoutCount);

            unsafe public Span<Format> Formats =>
                new Span<Format>((void*)_formats, _formatCount);

            unsafe public Span<int> SampleRates =>
                new Span<int>((void*)_sampleRates, _sampleRateCount);

            public bool IsRaw => _isRaw != 0;
            public Error ProbeError => _probeError;
        }

        public class Device : SafeHandleZeroOrMinusOneIsInvalid
        {
            private Device() : base(true) {}

            protected override bool ReleaseHandle()
            {
                Unref(this.handle);
                return true;
            }

            unsafe public ref DeviceData Data =>
                ref Unsafe.AsRef<DeviceData>((void*)this.handle);

            [DllImport("SoundIO.dll", EntryPoint="soundio_device_unref")]
            extern static void Unref(IntPtr device);
        }

        [DllImport("SoundIO.dll", EntryPoint="soundio_output_device_count")]
        public extern static int OutputDeviceCount(Context context);

        [DllImport("SoundIO.dll", EntryPoint="soundio_input_device_count")]
        public extern static int InputDeviceCount(Context context);

        [DllImport("SoundIO.dll", EntryPoint="soundio_get_input_device")]
        public extern static Device GetInputDevice(Context context, int index);

        [DllImport("SoundIO.dll", EntryPoint="soundio_get_output_device")]
        public extern static Device GetOutputDevice(Context context, int index);

        [DllImport("SoundIO.dll", EntryPoint="soundio_default_input_device_index")]
        public extern static int DefaultInputDeviceIndex(Context context);

        [DllImport("SoundIO.dll", EntryPoint="soundio_default_output_device_index")]
        public extern static int DefaultOutputDeviceIndex(Context context);

        [DllImport("SoundIO.dll", EntryPoint="soundio_device_equal")]
        [return: MarshalAs(UnmanagedType.U1)]
        public extern static bool Equal(Device a, Device b);

        [DllImport("SoundIO.dll", EntryPoint="soundio_device_sort_channel_layouts")]
        public extern static void SortChannelLayouts(Device device);

        [DllImport("SoundIO.dll", EntryPoint="soundio_device_supports_format")]
        [return: MarshalAs(UnmanagedType.U1)]
        public extern static bool SupportsFormat(Device device, Format format);

        [DllImport("SoundIO.dll", EntryPoint="soundio_device_supports_layout")]
        [return: MarshalAs(UnmanagedType.U1)]
        public extern static bool SupportsLayout(Device device, in ChannelLayout layout);

        [DllImport("SoundIO.dll", EntryPoint="soundio_device_supports_sample_rate")]
        [return: MarshalAs(UnmanagedType.U1)]
        public extern static bool SupportsSampleRate(Device device, int sampleRate);

        [DllImport("SoundIO.dll", EntryPoint="soundio_device_nearest_sample_rate")]
        public extern static int NearestSampleRate(Device device, int sampleRate);

        #endregion

        #region InStream operations

        [StructLayout(LayoutKind.Sequential)]
        public struct InStreamData
        {
            IntPtr _device;
            public Format Format;
            public int SampleRate;
            public ChannelLayout Layout;
            public double SoftwareLatency;
            public IntPtr UserData;
            IntPtr _readCallback;
            IntPtr _overflowCallback;
            IntPtr _errorCallback;
            IntPtr _name;
            byte _nonTerminalHint;
            public int BytesPerFrame;
            public int BytesPerSample;
            Error _layoutError;

            public delegate void ReadCallback(ref InStreamData stream, int frameCountMin, int frameCountMax);
            public delegate void OverflowCallback(ref InStreamData stream);
            public delegate void ErrorCallback(ref InStreamData stream, Error error);

            public ReadCallback OnRead
            {
                get => Marshal.GetDelegateForFunctionPointer<ReadCallback>(_readCallback);
                set => _readCallback = Marshal.GetFunctionPointerForDelegate(value);
            }

            public OverflowCallback OnOverflow
            {
                get => Marshal.GetDelegateForFunctionPointer<OverflowCallback>(_overflowCallback);
                set => _overflowCallback = Marshal.GetFunctionPointerForDelegate(value);
            }

            public ErrorCallback OnError
            {
                get => Marshal.GetDelegateForFunctionPointer<ErrorCallback>(_errorCallback);
                set => _errorCallback = Marshal.GetFunctionPointerForDelegate(value);
            }

            public bool NonTerminalHint
            {
                get => _nonTerminalHint != 0;
                set => _nonTerminalHint = value ? (byte)1 : (byte)0;
            }

            public string Name => Marshal.PtrToStringAnsi(_name);

            public Error LayoutError => _layoutError;
        }

        public class InStream : SafeHandleZeroOrMinusOneIsInvalid
        {
            private InStream() : base(true) {}

            protected override bool ReleaseHandle()
            {
                Destroy(this.handle);
                return true;
            }

            unsafe public ref InStreamData Data =>
                ref Unsafe.AsRef<InStreamData>((void*)this.handle);

            [DllImport("SoundIO.dll", EntryPoint="soundio_instream_destroy")]
            extern static void Destroy(IntPtr stream);
        }

        [DllImport("SoundIO.dll", EntryPoint="soundio_instream_create")]
        public extern static InStream InStreamCreate(Device device);

        [DllImport("SoundIO.dll", EntryPoint="soundio_instream_open")]
        public extern static Error Open(InStream stream);

        [DllImport("SoundIO.dll", EntryPoint="soundio_instream_start")]
        public extern static Error Start(InStream stream);

        [DllImport("SoundIO.dll", EntryPoint="soundio_instream_begin_read")]
        unsafe public extern static Error
            BeginRead(ref InStreamData stream, out ChannelArea* areas, ref int frameCount);

        [DllImport("SoundIO.dll", EntryPoint="soundio_instream_end_read")]
        public extern static Error EndRead(ref InStreamData stream);

        [DllImport("SoundIO.dll", EntryPoint="soundio_instream_pause")]
        public extern static Error
            Pause(InStream stream, [MarshalAs(UnmanagedType.U1)] bool pause);

        [DllImport("SoundIO.dll", EntryPoint="soundio_instream_get_latency")]
        public extern static Error GetLatency(InStream stream, out double latency);

        #endregion

        #region Ring buffer operations

        public class RingBuffer : SafeHandleZeroOrMinusOneIsInvalid
        {
            private RingBuffer() : base(true) {}

            protected override bool ReleaseHandle()
            {
                Destroy(this.handle);
                return true;
            }

            [DllImport("SoundIO.dll", EntryPoint="soundio_ring_buffer_destroy")]
            extern static void Destroy(IntPtr buffer);
        }

        [DllImport("SoundIO.dll", EntryPoint="soundio_ring_buffer_create")]
        public extern static RingBuffer RingBufferCreate(Context context, int capacity);

        [DllImport("SoundIO.dll", EntryPoint="soundio_ring_buffer_capacity")]
        public extern static int Capacity(RingBuffer buffer);

        [DllImport("SoundIO.dll", EntryPoint="soundio_ring_buffer_write_ptr")]
        public extern static IntPtr WritePtr(RingBuffer buffer);

        [DllImport("SoundIO.dll", EntryPoint="soundio_ring_buffer_advance_write_ptr")]
        public extern static void AdvanceWritePtr(RingBuffer buffer, int count);

        [DllImport("SoundIO.dll", EntryPoint="soundio_ring_buffer_read_ptr")]
        public extern static IntPtr ReadPtr(RingBuffer buffer);

        [DllImport("SoundIO.dll", EntryPoint="soundio_ring_buffer_advance_read_ptr")]
        public extern static void AdvanceReadPtr(RingBuffer buffer, int count);

        [DllImport("SoundIO.dll", EntryPoint="soundio_ring_buffer_fill_count")]
        public extern static int FillCount(RingBuffer buffer);

        [DllImport("SoundIO.dll", EntryPoint="soundio_ring_buffer_free_count")]
        public extern static int FreeCount(RingBuffer buffer);

        [DllImport("SoundIO.dll", EntryPoint="soundio_ring_buffer_clear")]
        public extern static void Clear(RingBuffer buffer);

        #endregion

        #region Thread factory replacement

        [StructLayout(LayoutKind.Sequential)]
        public struct ThreadFactoryData
        {
            IntPtr _createThread;
            IntPtr _destroyThread;

            public delegate IntPtr CreateThreadDelegate(IntPtr userData);
            public delegate void DestroyThreadDelegate(IntPtr thread);

            public CreateThreadDelegate CreateThread
            {
                get => Marshal.GetDelegateForFunctionPointer<CreateThreadDelegate>(_createThread);
                set => _createThread = Marshal.GetFunctionPointerForDelegate(value);
            }

            public DestroyThreadDelegate DestroyThread
            {
                get => Marshal.GetDelegateForFunctionPointer<DestroyThreadDelegate>(_destroyThread);
                set => _destroyThread = Marshal.GetFunctionPointerForDelegate(value);
            }
        }

        [DllImport("SoundIO.dll", EntryPoint="soundio_set_thread_factory")]
        public extern static void SetThreadFactory(in ThreadFactoryData factory);

        [DllImport("SoundIO.dll", EntryPoint="soundio_run_thread")]
        public extern static void RunThread(IntPtr userData);

        #endregion
    }
}
