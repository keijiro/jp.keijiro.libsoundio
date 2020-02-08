using System.Runtime.InteropServices;

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

    unsafe public static class Unmanaged
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

        const int MAX_CHANNELS = 24;

        [StructLayout(LayoutKind.Sequential)]
        public struct ChannelLayout
        {
            public byte* name;
            public int channelCount;
            public fixed int channels[MAX_CHANNELS];
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SampleRateRange
        {
            public int min;
            public int max;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ChannelArea
        {
            public byte* ptr;
            public int step;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Instance
        {
            public void* userdata;
            void* onDevicesChange;
            void* onBackendDisconnect;
            void* onEventsSignal;
            public Backend currentBackend;
            public byte* appName;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Device
        {
            Instance* instance;

            public byte* id;
            public byte* name;

            public DeviceAim aim;

            public ChannelLayout* layouts;
            public int layoutCount;
            public ChannelLayout currentLayout;

            public Format* formats;
            public int formatCount;
            public Format currentFormat;

            public SampleRateRange* sampleRates;
            public int sampleRateCount;
            public int sampleRateCurent;

            public double softwareLatencyMin;
            public double softwareLatencyMax;
            public double softwareLatencyCurrent;

            [MarshalAs(UnmanagedType.U1)]
            public bool isRaw;
            public int refCount;
            public Error probeError;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct OutStream
        {
            public Device* device;
            public Format format;
            public int sampleRate;
            public ChannelLayout layout;
            public double softwareLatency;
            public float volume;
            public void* userData;

            public void* writeCallback;
            public void* underflowCallback;
            public void* errorCallback;

            public byte* name;
            [MarshalAs(UnmanagedType.U1)]
            public bool nonTerminalHint;

            public int bytesPerFrame;
            public int bytesPerSample;
            public int layoutError;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct InStream
        {
            public Device* device;
            public Format format;
            public int sampleRate;
            public ChannelLayout layout;
            public double softwareLatency;
            public void* userData;

            public System.IntPtr readCallback;
            public System.IntPtr overflowCallback;
            public System.IntPtr errorCallback;

            public byte* name;
            [MarshalAs(UnmanagedType.U1)]
            public bool nonTerminalHint;

            public int bytesPerFrame;
            public int bytesPerSample;
            public int layoutError;
        }

        public struct RingBuffer { }

        public delegate void ReadCallback(InStream* stream, int frameCountMin, int frameCountMax);
        public delegate void OverflowCallback(InStream* stream);
        public delegate void ErrorCallback(InStream* stream, Error error);

        [DllImport("libsoundio.dll", EntryPoint="soundio_create")]
        public extern static Instance* Create();

        [DllImport("libsoundio.dll", EntryPoint="soundio_destroy")]
        public extern static void Destroy(Instance* instance);

        [DllImport("libsoundio.dll", EntryPoint="soundio_connect")]
        public extern static Error Connect(Instance* instance);

        [DllImport("libsoundio.dll", EntryPoint="soundio_connect_backend")]
        public extern static Error Connect(Instance* instance, Backend backend);

        [DllImport("libsoundio.dll", EntryPoint="soundio_disconnect")]
        public extern static void Disconnect(Instance* instance);

        [DllImport("libsoundio.dll", EntryPoint="soundio_flush_events")]
        public extern static void FlushEvents(Instance* instance);

        [DllImport("libsoundio.dll", EntryPoint="soundio_output_device_count")]
        public extern static int OutputDeviceCount(Instance* instance);

        [DllImport("libsoundio.dll", EntryPoint="soundio_input_device_count")]
        public extern static int InputDeviceCount(Instance* instance);

        [DllImport("libsoundio.dll", EntryPoint="soundio_default_output_device_index")]
        public extern static int DefaultOutputDeviceIndex(Instance* instance);

        [DllImport("libsoundio.dll", EntryPoint="soundio_default_input_device_index")]
        public extern static int DefaultInputDeviceIndex(Instance* instance);

        [DllImport("libsoundio.dll", EntryPoint="soundio_get_input_device")]
        public extern static Device* GetInputDevice(Instance* instance, int index);

        [DllImport("libsoundio.dll", EntryPoint="soundio_get_output_device")]
        public extern static Device* GetOutputDevice(Instance* instance, int index);

        [DllImport("libsoundio.dll", EntryPoint="soundio_device_unref")]
        public extern static void UnrefDevice(Device* device);

        [DllImport("libsoundio.dll", EntryPoint="soundio_device_sort_channel_layouts")]
        public extern static void SortChannelLayouts(Device* device);

        [DllImport("libsoundio.dll", EntryPoint="soundio_device_supports_format")]
        [return: MarshalAs(UnmanagedType.U1)]
        public extern static bool SupportsFormat(Device* device, Format format);

        [DllImport("libsoundio.dll", EntryPoint="soundio_device_supports_sample_rate")]
        [return: MarshalAs(UnmanagedType.U1)]
        public extern static bool SupportsSampleRate(Device* device, int sampleRate);

        [DllImport("libsoundio.dll", EntryPoint="soundio_device_nearest_sample_rate")]
        public extern static int NearestSampleRate(Device* device, int sampleRate);

        [DllImport("libsoundio.dll", EntryPoint="soundio_channel_layout_builtin_count")]
        public extern static int ChannelLayoutBuiltinCount();

        [DllImport("libsoundio.dll", EntryPoint="soundio_channel_layout_get_builtin")]
        public extern static ChannelLayout* ChannelLayoutGetBuiltin(ChannelLayoutID id);

        #region InStream operations

        [DllImport("libsoundio.dll", EntryPoint="soundio_instream_create")]
        public extern static InStream* InStreamCreate(Device* device);

        [DllImport("libsoundio.dll", EntryPoint="soundio_instream_destroy")]
        public extern static void Destroy(InStream* stream);

        [DllImport("libsoundio.dll", EntryPoint="soundio_instream_open")]
        public extern static Error Open(InStream* stream);

        [DllImport("libsoundio.dll", EntryPoint="soundio_instream_start")]
        public extern static Error Start(InStream* stream);

        #endregion

        #region Ring buffer operations

        [DllImport("libsoundio.dll", EntryPoint="soundio_ring_buffer_create")]
        public extern static RingBuffer* RingBufferCreate(Instance* instance, int capacity);

        [DllImport("libsoundio.dll", EntryPoint="soundio_ring_buffer_destroy")]
        public extern static void Destroy(RingBuffer* buffer);

        [DllImport("libsoundio.dll", EntryPoint="soundio_ring_buffer_capacity")]
        public extern static int Capacity(RingBuffer* buffer);

        [DllImport("libsoundio.dll", EntryPoint="soundio_ring_buffer_write_ptr")]
        public extern static byte* WritePtr(RingBuffer* buffer);

        [DllImport("libsoundio.dll", EntryPoint="soundio_ring_buffer_advance_write_ptr")]
        public extern static void AdvanceWritePtr(RingBuffer* buffer, int count);

        [DllImport("libsoundio.dll", EntryPoint="soundio_ring_buffer_read_ptr")]
        public extern static byte* ReadPtr(RingBuffer* buffer);

        [DllImport("libsoundio.dll", EntryPoint="soundio_ring_buffer_advance_read_ptr")]
        public extern static void AdvanceReadPtr(RingBuffer* buffer, int count);

        [DllImport("libsoundio.dll", EntryPoint="soundio_ring_buffer_fill_count")]
        public extern static int FillCount(RingBuffer* buffer);

        [DllImport("libsoundio.dll", EntryPoint="soundio_ring_buffer_free_count")]
        public extern static int FreeCount(RingBuffer* buffer);

        [DllImport("libsoundio.dll", EntryPoint="soundio_ring_buffer_clear")]
        public extern static void Clear(RingBuffer* buffer);

        #endregion
    }
}
