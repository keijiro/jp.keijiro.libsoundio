// libsoundio C# thin wrapper class library
// https://github.com/keijiro/jp.keijiro.libsoundio

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace SoundIO
{
    // SoundIoDevice struct wrapper class
    public class Device : SafeHandleZeroOrMinusOneIsInvalid
    {
        #region SoundIoDevice struct representation

        [StructLayout(LayoutKind.Sequential)]
        internal struct NativeData
        {
            internal IntPtr context;
            internal IntPtr id;
            internal IntPtr name;
            internal DeviceAim aim;

            internal IntPtr layouts;
            internal int layoutCount;
            internal ChannelLayout currentLayout;

            internal IntPtr formats;
            internal int formatCount;
            internal Format currentFormat;

            internal IntPtr sampleRates;
            internal int sampleRateCount;
            internal int currentSampleRate;

            internal double softwareLatencyMin;
            internal double softwareLatencyMax;
            internal double softwareLatencyCurrent;

            internal byte isRaw;
            internal int refCount;
            internal Error probeError;
        }

        #endregion

        #region SafeHandle implementation

        Device() : base(true) {}

        protected override bool ReleaseHandle()
        {
            _Unref(handle);
            return true;
        }

        unsafe ref NativeData Data => ref Unsafe.AsRef<NativeData>((void*)handle);

        #endregion

        #region Struct member accessors

        public string ID => Marshal.PtrToStringAnsi(Data.id);
        public string Name => Marshal.PtrToStringAnsi(Data.name);
        public DeviceAim Aim => Data.aim;

        unsafe public Span<ChannelLayout> Layouts =>
            new Span<ChannelLayout>((void*)Data.layouts, Data.layoutCount);

        public ChannelLayout CurrentLayout => Data.currentLayout;

        unsafe public Span<Format> Formats =>
            new Span<Format>((void*)Data.formats, Data.formatCount);

        public Format CurrentFormat => Data.currentFormat;

        unsafe public Span<int> SampleRates =>
            new Span<int>((void*)Data.sampleRates, Data.sampleRateCount);

        public int CurrentSampleRate => Data.currentSampleRate;

        public double SoftwareLatencyMin => Data.softwareLatencyMin;
        public double SoftwareLatencyMax => Data.softwareLatencyMax;
        public double SoftwareLatencyCurrent => Data.softwareLatencyCurrent;

        public bool IsRaw => Data.isRaw != 0;
        public Error ProbeError => Data.probeError;

        #endregion

        #region Public properties and methods

        public bool Equal(Device rhs) => _Equal(this, rhs) != 0;
        public void SortChannelLayouts() => _SortChannelLayouts(this);
        public bool CheckSupport(Format format) => _SupportsFormat(this, format) != 0;
        public bool CheckSupport(in ChannelLayout layout) => _SupportsLayout(this, layout) != 0;
        public bool CheckSampleRateSupport(int rate) => _SupportsSampleRate(this, rate) != 0;
        public int GetNearestSampleRate(int rate) => _NearestSampleRate(this, rate);

        #endregion

        #region Unmanaged functions

        [DllImport(Config.DllName, EntryPoint="soundio_device_unref")]
        extern static void _Unref(IntPtr device);

        [DllImport(Config.DllName, EntryPoint="soundio_device_equal")]
        extern static byte _Equal(Device a, Device b);

        [DllImport(Config.DllName, EntryPoint="soundio_device_sort_channel_layouts")]
        extern static void _SortChannelLayouts(Device device);

        [DllImport(Config.DllName, EntryPoint="soundio_device_supports_format")]
        extern static byte _SupportsFormat(Device device, Format format);

        [DllImport(Config.DllName, EntryPoint="soundio_device_supports_layout")]
        extern static byte _SupportsLayout(Device device, in ChannelLayout layout);

        [DllImport(Config.DllName, EntryPoint="soundio_device_supports_sample_rate")]
        extern static byte _SupportsSampleRate(Device device, int sampleRate);

        [DllImport(Config.DllName, EntryPoint="soundio_device_nearest_sample_rate")]
        extern static int _NearestSampleRate(Device device, int sampleRate);

        #endregion
    }
}
