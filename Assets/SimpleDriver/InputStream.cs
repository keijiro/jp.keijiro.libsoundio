// Simple example driver for soundio
// https://github.com/keijiro/jp.keijiro.libsoundio

using System;
using System.Runtime.InteropServices;
using InvalidOp = System.InvalidOperationException;

namespace SoundIO.SimpleDriver
{
    //
    // High-level wrapper class for SoundIoInStream
    //
    // - Manages an InStream object.
    // - Manages the sound input ring buffer.
    // - Provides the "last frame window" that exposes incoming sound data that
    //   was received in the last frame.
    // - Implements callback functions for the InStream object.
    //
    // Note that the last frame window doesn't provide the data for the ACTUAL
    // last frame. There must be latency caused by software/hardware.
    //
    public sealed class InputStream : IDisposable
    {
        #region Public properties and methods

        public int ChannelCount => _stream.Layout.ChannelCount;
        public int SampleRate => _stream.SampleRate;
        public float Latency => (float)_stream.SoftwareLatency;

        public bool IsValid =>
            _stream != null && !_stream.IsInvalid && !_stream.IsClosed;

        public ReadOnlySpan<byte> LastFrameWindow =>
            new ReadOnlySpan<byte>(_window, 0, _windowSize);

        public void Dispose()
        {
            _stream?.Dispose();
            _device?.Dispose();
            _self.Free();
        }

        public void Update()
        {
            // Last frame window size
            var dt = UnityEngine.Time.deltaTime;
            _windowSize = Math.Min(_window.Length, CalculateBufferSize(dt));

            lock (_ring)
            {
                // Copy the last frame data into the window buffer.
                if (_ring.FillCount > _windowSize)
                    _ring.Read(new Span<byte>(_window, 0, _windowSize));

                // Reset the buffer if it's overflowed in the last frame.
                if (_ring.OverflowCount > 0) _ring.Clear();
            }
        }

        #endregion

        #region Constructor

        public InputStream(Device deviceToOwn)
        {
            _self = GCHandle.Alloc(this);
            _device = deviceToOwn;
            _stream = InStream.Create(_device);

            try
            {
                if (_stream.IsInvalid)
                    throw new InvalidOp("Stream allocation error");

                if (_device.Layouts.Length == 0)
                    throw new InvalidOp("No channel layout");

                // Calculate the best latency. FIXME: Use target frame rate?
                var bestLatency = Math.Max(1.0 / 60, _device.SoftwareLatencyMin);

                // Stream properties
                _stream.Format = Format.Float32LE;
                _stream.Layout = _device.Layouts[0];
                _stream.SoftwareLatency = bestLatency;
                _stream.ReadCallback = _readCallback;
                _stream.OverflowCallback = _overflowCallback;
                _stream.ErrorCallback = _errorCallback;
                _stream.UserData = GCHandle.ToIntPtr(_self);

                var err = _stream.Open();

                if (err != Error.None)
                    throw new InvalidOp($"Stream initialization error ({err})");

                // Determine the buffer size from the actual software latency.
                var latency = Math.Max(_stream.SoftwareLatency, bestLatency);
                var bufferSize = CalculateBufferSize((float)(latency * 4));

                // Ring/window buffer allocation
                _ring = new RingBuffer(bufferSize);
                _window = new byte[bufferSize];

                // Start streaming.
                _stream.Start();
            }
            catch
            {
                // Dispose resources on exception.
                _stream.Dispose();
                _device.Dispose();
                _stream = null;
                _device = null;
                throw;
            }
        }

        #endregion

        #region Internal objects

        // GC handle used to share 'this' pointer with unmanaged code
        GCHandle _self;

        // SoundIO objects
        Device _device;
        InStream _stream;

        // Input stream ring buffer
        // This object will be accessed from both the main and callback thread.
        // Must be locked when using.
        RingBuffer _ring;

        // Buffer for last frame window
        byte[] _window;
        int _windowSize;

        #endregion

        #region Internal function

        int CalculateBufferSize(float second) =>
            sizeof(float) * (int)(ChannelCount * SampleRate * second);

        #endregion

        #region SoundIO callback delegates

        static InStream.ReadCallbackDelegate _readCallback = OnReadInStream;
        static InStream.OverflowCallbackDelegate _overflowCallback = OnOverflowInStream;
        static InStream.ErrorCallbackDelegate _errorCallback = OnErrorInStream;

        [AOT.MonoPInvokeCallback(typeof(InStream.ReadCallbackDelegate))]
        unsafe static void OnReadInStream(ref InStreamData stream, int min, int left)
        {
            // Recover the 'this' reference from the UserData pointer.
            var self = (InputStream)GCHandle.FromIntPtr(stream.UserData).Target;

            while (left > 0)
            {
                // Start reading the buffer.
                var count = left;
                ChannelArea* areas;
                stream.BeginRead(out areas, ref count);

                // When getting count == 0, we must stop reading
                // immediately without calling InStream.EndRead.
                if (count == 0) break;

                if (areas == null)
                {
                    // We must do zero-fill when receiving a null pointer.
                    lock (self._ring)
                        self._ring.WriteEmpty(stream.BytesPerFrame * count);
                }
                else
                {
                    // Determine the memory span of the input data with
                    // assuming the data is tightly packed.
                    // TODO: Is this assumption always true?
                    var span = new ReadOnlySpan<Byte>(
                        (void*)areas[0].Pointer,
                        areas[0].Step * count
                    );

                    // Transfer the data to the ring buffer.
                    lock (self._ring) self._ring.Write(span);
                }

                stream.EndRead();

                left -= count;
            }
        }

        [AOT.MonoPInvokeCallback(typeof(InStream.OverflowCallbackDelegate))]
        static void OnOverflowInStream(ref InStreamData stream)
        {
            UnityEngine.Debug.LogWarning("InStream overflow");
        }

        [AOT.MonoPInvokeCallback(typeof(InStream.ErrorCallbackDelegate))]
        static void OnErrorInStream(ref InStreamData stream, Error error)
        {
            UnityEngine.Debug.LogError($"InStream error ({error})");
        }

        #endregion
    }
}
