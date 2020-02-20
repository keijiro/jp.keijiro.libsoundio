using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;

using Debug = UnityEngine.Debug;

namespace UnitySioTest
{
    public sealed class SoundIODriver : UnityEngine.MonoBehaviour
    {
        #region Public properties and methods

        public int DeviceCount => _validDevices.Count;
        public int ChannelCount => _dev.CurrentLayout.ChannelCount;

        public ReadOnlyCollection<string> GetDeviceNameList() =>
            _validDevices.Select(pair => pair.name).ToList().AsReadOnly();

        public void SelectDevice(int index)
        {
            CloseCurrentDevice();

            // Open the given device.
            _dev = _sio.GetInputDevice(_validDevices[index].index);

            if (!IsValid(_dev))
            {
                Debug.LogError("Failed to open an input device.");
                return;
            }

            if (_dev.ProbeError != SoundIO.Error.None)
            {
                Debug.LogError("Unable to probe device ({_dev.ProbeError})");
                return;
            }

            _dev.SortChannelLayouts();

            // Create an input stream.
            _ins = SoundIO.InStream.Create(_dev);

            if (!IsValid(_ins))
            {
                Debug.LogError("Failed to create an input stream.");
                return;
            }

            _ins.Format = SoundIO.Format.Float32LE;
            _ins.SoftwareLatency = 1.0 / 60;
            _ins.ReadCallback = _readCallback;
            _ins.OverflowCallback = _overflowCallback;
            _ins.ErrorCallback = _errorCallback;
            _ins.UserData = GCHandle.ToIntPtr(_self);

            var err = _ins.Open();

            if (err != SoundIO.Error.None)
            {
                Debug.LogError($"Falied to open an input stream ({err})");
                return;
            }

            _ins.Start();
        }

        public void CloseCurrentDevice()
        {
            if (IsValid(_ins)) _ins.Close();
            if (IsValid(_dev)) _dev.Close();
        }

        public bool TryReadInput(Span<byte> dest)
        {
            var read = false;

            lock (_ring)
            {
                while (_ring.FillCount > dest.Length)
                {
                    _ring.Read(dest);
                    read = true;
                }
            }

            return read;
        }

        #endregion

        #region Internal objects

        // A handle used to share 'this' pointer with DLL
        GCHandle _self;

        // Safe handles
        SoundIO.Context _sio;
        SoundIO.Device _dev;
        SoundIO.InStream _ins;

        // Device list containing only valid ones
        List<(int index, string name)> _validDevices = new List<(int, string)>();

        // Input stream ring buffer
        RingBuffer _ring = new RingBuffer(256 * 1024);

        #endregion

        #region MonoBehaviour implementation

        void Start()
        {
            _self = GCHandle.Alloc(this);

            // SoundIO context initialization
            _sio = SoundIO.Context.Create();

            if (!IsValid(_sio))
            {
                Debug.LogError("Failed to create a soundio context.");
                return;
            }

            _sio.Connect();
            _sio.FlushEvents();

            // Valid device enumeration
            for (var i = 0; i < _sio.InputDeviceCount; i++)
                using(var dev = _sio.GetInputDevice(i))
                    if (IsValidDevice(dev)) _validDevices.Add((i, dev.Name));
        }

        void OnDestroy()
        {
            if (IsValid(_ins)) _ins.Close();
            if (IsValid(_dev)) _dev.Close();
            if (IsValid(_sio)) _sio.Close();
            _self.Free();
        }

        void Update()
        {
            if (IsValid(_sio)) _sio.FlushEvents();
        }

        #endregion

        #region Validator methods

        bool IsValid(SafeHandle handle) => handle != null && !handle.IsInvalid;

        bool IsValidDevice(SoundIO.Device device)
        {
            return
                // We prefer non-raw devices.
                !device.IsRaw &&
                // It should have at least one channel.
                device.CurrentLayout.ChannelCount > 0 &&
                // It should support 48kHz sampling rate.
                device.SampleRates.ToArray().Contains(48000) &&
                // It should support the float 32-bit little ending format.
                device.Formats.ToArray().Contains(SoundIO.Format.Float32LE);
        }

        #endregion

        #region SoundIO callback delegates

        static SoundIO.InStream.ReadCallbackDelegate _readCallback = OnReadInStream;
        static SoundIO.InStream.OverflowCallbackDelegate _overflowCallback = OnOverflowInStream;
        static SoundIO.InStream.ErrorCallbackDelegate _errorCallback = OnErrorInStream;

        [AOT.MonoPInvokeCallback(typeof(SoundIO.InStream.ReadCallbackDelegate))]
        unsafe static void OnReadInStream(ref SoundIO.InStreamData stream, int frameMin, int frameMax)
        {
            // Recover the 'this' reference from the UserData pointer.
            var self = (SoundIODriver)GCHandle.FromIntPtr(stream.UserData).Target;

            // Receive the input data as much as possible.
            for (var left = frameMax; left > 0;)
            {
                // Start reading the buffer.
                var frameCount = left;
                SoundIO.ChannelArea* areas;
                stream.BeginRead(out areas, ref frameCount);

                // When getting frameCount == 0, we must stop reading
                // immediately without calling InStream.EndRead.
                if (frameCount == 0) break;

                if (areas == null)
                {
                    // We must do zero-fill when receiving a null pointer.
                    lock (self._ring)
                        self._ring.WriteEmpty(frameCount * stream.BytesPerFrame);
                }
                else
                {
                    // Determine the memory span of the input data with
                    // assuming the data is tightly packed.
                    // TODO: Is this assumption always true?
                    var span = new ReadOnlySpan<Byte>(
                        (void*)areas[0].Pointer,
                        areas[0].Step * frameCount
                    );

                    // Transfer the data to the ring buffer.
                    lock (self._ring) self._ring.Write(span);
                }

                stream.EndRead();

                left -= frameCount;
            }
        }

        [AOT.MonoPInvokeCallback(typeof(SoundIO.InStream.OverflowCallbackDelegate))]
        static void OnOverflowInStream(ref SoundIO.InStreamData stream)
        {
            Debug.LogWarning("InStream overflow");
        }

        [AOT.MonoPInvokeCallback(typeof(SoundIO.InStream.ErrorCallbackDelegate))]
        static void OnErrorInStream(ref SoundIO.InStreamData stream, SoundIO.Error error)
        {
            Debug.LogError($"InStream error ({error})");
        }

        #endregion
    }
}
