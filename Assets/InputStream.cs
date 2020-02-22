using System;
using System.Runtime.InteropServices;
using InvalidOp = System.InvalidOperationException;

namespace UnitySioTest
{
    public sealed class InputStream : IDisposable
    {
        #region Public properties and methods

        public int ChannelCount => _stream.Layout.ChannelCount;
        public int SampleRate => _stream.SampleRate;
        public float Latency => (float)_stream.SoftwareLatency;

        public bool IsValid =>
            _stream != null && !_stream.IsClosed && !_stream.IsInvalid;

        public ReadOnlySpan<byte> LastFrameWindow =>
            new ReadOnlySpan<byte>(_window, 0, _windowSize);

        public void Dispose()
        {
            if (Validate(_stream)) _stream.Dispose();
            if (Validate(_device)) _device.Dispose();
            _self.Free();
        }

        public void Update()
        {
            // Last frame window calculation
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

        public InputStream(SoundIO.Device deviceToOwn)
        {
            _self = GCHandle.Alloc(this);
            _device = deviceToOwn;
            _stream = SoundIO.InStream.Create(_device);

            try
            {
                if (!Validate(_stream))
                    throw new InvalidOp("Stream allocation error");

                if (_device.Layouts.Length == 0)
                    throw new InvalidOp("No channel layout");

                // Calculate the best latency. FIXME: Use target frame rate?
                var bestLatency = Math.Max(1.0 / 60, _device.SoftwareLatencyMin);

                // Stream properties
                _stream.Format = SoundIO.Format.Float32LE;
                _stream.Layout = _device.Layouts[0];
                _stream.SoftwareLatency = bestLatency;
                _stream.ReadCallback = _readCallback;
                _stream.OverflowCallback = _overflowCallback;
                _stream.ErrorCallback = _errorCallback;
                _stream.UserData = GCHandle.ToIntPtr(_self);

                var err = _stream.Open();

                if (err != SoundIO.Error.None)
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
                throw;
            }
        }

        #endregion

        #region Internal objects

        // A handle used to share 'this' pointer with DLL
        GCHandle _self;

        // Safe handles
        SoundIO.Device _device;
        SoundIO.InStream _stream;

        // Input stream ring buffer
        RingBuffer _ring;

        // Single frame window
        byte[] _window;
        int _windowSize;

        #endregion

        #region Internal methods

        bool Validate(SafeHandle handle) =>
            handle != null && !handle.IsInvalid && !handle.IsClosed;

        int CalculateBufferSize(float second) =>
            sizeof(float) * (int)(ChannelCount * SampleRate * second);

        #endregion

        #region SoundIO callback delegates

        static SoundIO.InStream.ReadCallbackDelegate _readCallback = OnReadInStream;
        static SoundIO.InStream.OverflowCallbackDelegate _overflowCallback = OnOverflowInStream;
        static SoundIO.InStream.ErrorCallbackDelegate _errorCallback = OnErrorInStream;

        [AOT.MonoPInvokeCallback(typeof(SoundIO.InStream.ReadCallbackDelegate))]
        unsafe static void OnReadInStream(ref SoundIO.InStreamData stream, int frameMin, int frameMax)
        {
            // Recover the 'this' reference from the UserData pointer.
            var self = (InputStream)GCHandle.FromIntPtr(stream.UserData).Target;

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
            UnityEngine.Debug.LogWarning("InStream overflow");
        }

        [AOT.MonoPInvokeCallback(typeof(SoundIO.InStream.ErrorCallbackDelegate))]
        static void OnErrorInStream(ref SoundIO.InStreamData stream, SoundIO.Error error)
        {
            UnityEngine.Debug.LogError($"InStream error ({error})");
        }

        #endregion
    }
}
