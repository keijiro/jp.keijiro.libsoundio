using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace SoundIO
{
    //
    // SoundIo struct wrapper class
    //
    public class Context : SafeHandleZeroOrMinusOneIsInvalid
    {
        #region SoundIo struct representation

        [StructLayout(LayoutKind.Sequential)]
        internal struct NativeData
        {
            internal IntPtr userdata;
            internal IntPtr onDevicesChange;
            internal IntPtr onBackendDisconnect;
            internal IntPtr onEventsSignal;
            internal Backend currentBackend;
            internal IntPtr appName;
        }

        #endregion

        #region SafeHandle implementation

        Context() : base(true) {}

        protected override bool ReleaseHandle()
        {
            _Destroy(handle);
            return true;
        }

        unsafe ref NativeData Data => ref Unsafe.AsRef<NativeData>((void*)handle);

        #endregion

        #region Struct member accessors

        public IntPtr UserData => Data.userdata;

        public delegate void OnDevicesChangeDelegate(IntPtr context);

        public OnDevicesChangeDelegate OnDevicesChange
        {
            get => Marshal.GetDelegateForFunctionPointer<OnDevicesChangeDelegate>(Data.onDevicesChange);
            set => Data.onDevicesChange = Marshal.GetFunctionPointerForDelegate(value);
        }

        public delegate void OnBackendDisconnectDelegate(IntPtr context, Error error);

        public OnBackendDisconnectDelegate OnBackendDisconnect
        {
            get => Marshal.GetDelegateForFunctionPointer<OnBackendDisconnectDelegate>(Data.onBackendDisconnect);
            set => Data.onBackendDisconnect = Marshal.GetFunctionPointerForDelegate(value);
        }

        public delegate void OnEventsSignalDelegate(IntPtr context);

        public OnEventsSignalDelegate OnEventSignal
        {
            get => Marshal.GetDelegateForFunctionPointer<OnEventsSignalDelegate>(Data.onEventsSignal);
            set => Data.onEventsSignal = Marshal.GetFunctionPointerForDelegate(value);
        }

        public Backend CurrentBackend => Data.currentBackend;
        public string AppName => Marshal.PtrToStringAnsi(Data.appName);

        #endregion

        #region Public properties and methods

        static public Context Create() => _Create();

        public Error Connect() => _Connect(this);
        public Error Connect(Backend backend) => _Connect(this, backend);
        public void Disconnect() => _Disconnect(this);
        public void FlushEvents() => _FlushEvents(this);

        public int OutputDeviceCount => _OutputDeviceCount(this);
        public int InputDeviceCount => _InputDeviceCount(this);
        public Device GetInputDevice(int index) => _GetInputDevice(this, index);
        public Device GetOutputDevice(int index) => _GetOutputDevice(this, index);
        public int DefaultInputDeviceIndex => _DefaultInputDeviceIndex(this);
        public int DefaultOutputDeviceIndex => _DefaultOutputDeviceIndex(this);

        #endregion

        #region Native methods

        [DllImport(Config.DllName, EntryPoint="soundio_create")]
        extern static Context _Create();

        [DllImport(Config.DllName, EntryPoint="soundio_destroy")]
        extern static void _Destroy(IntPtr context);

        [DllImport(Config.DllName, EntryPoint="soundio_connect")]
        extern static Error _Connect(Context context);

        [DllImport(Config.DllName, EntryPoint="soundio_connect_backend")]
        extern static Error _Connect(Context context, Backend backend);

        [DllImport(Config.DllName, EntryPoint="soundio_disconnect")]
        extern static void _Disconnect(Context context);

        [DllImport(Config.DllName, EntryPoint="soundio_flush_events")]
        extern static void _FlushEvents(Context context);

        [DllImport(Config.DllName, EntryPoint="soundio_output_device_count")]
        extern static int _OutputDeviceCount(Context context);

        [DllImport(Config.DllName, EntryPoint="soundio_input_device_count")]
        extern static int _InputDeviceCount(Context context);

        [DllImport(Config.DllName, EntryPoint="soundio_get_input_device")]
        extern static Device _GetInputDevice(Context context, int index);

        [DllImport(Config.DllName, EntryPoint="soundio_get_output_device")]
        extern static Device _GetOutputDevice(Context context, int index);

        [DllImport(Config.DllName, EntryPoint="soundio_default_input_device_index")]
        extern static int _DefaultInputDeviceIndex(Context context);

        [DllImport(Config.DllName, EntryPoint="soundio_default_output_device_index")]
        extern static int _DefaultOutputDeviceIndex(Context context);

        #endregion
    }
}
