using System;
using UnityEngine;
using GCHandle = System.Runtime.InteropServices.GCHandle;
using MemoryMarshal = System.Runtime.InteropServices.MemoryMarshal;

public sealed class Test : MonoBehaviour
{
    #region Editable attributes

    [SerializeField] string _deviceName = "";

    #endregion

    #region Internal objects

    GCHandle _self;

    SoundIO.Context _sio;
    SoundIO.Device _dev;
    SoundIO.InStream _ins;

    RingBuffer _ring = new RingBuffer(128 * 1024);
    byte[] _tempBuffer;

    #endregion

    #region MonoBehaviour implementation

    void Start()
    {
        _self = GCHandle.Alloc(this);

        _sio = SoundIO.Context.Create();

        if (_sio?.IsInvalid ?? true)
        {
            Debug.LogError("Can't create soundio context.");
            return;
        }

        _sio.Connect();
        _sio.FlushEvents();

        for (var i = 0; i < _sio.InputDeviceCount; i++)
        {
            _dev = _sio.GetInputDevice(i);
            if (_dev.Name == _deviceName) break;
            _dev.Close();
        }

        if (_dev.IsClosed)
            _dev = _sio.GetInputDevice(_sio.DefaultInputDeviceIndex);

        if (_dev?.IsInvalid ?? true)
        {
            Debug.LogError("Can't open the default input device.");
            return;
        }

        if (_dev.ProbeError != SoundIO.Error.None)
        {
            Debug.LogError("Unable to probe device ({_dev.ProbeError})");
            return;
        }

        _dev.SortChannelLayouts();

        _ins = SoundIO.InStream.Create(_dev);

        if (_ins?.IsInvalid ?? true)
        {
            Debug.LogError("Can't create an input stream.");
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
            Debug.LogError($"Can't open an input stream ({err})");
            return;
        }

        _ins.Start();
    }

    void Update()
    {
        if (!_sio?.IsInvalid ?? false) _sio.FlushEvents();

        var wr = GetComponent<WaveformRenderer>();
        if (wr == null) return;

        var dataSize = wr.BufferSize * sizeof(float);

        if (_tempBuffer == null || _tempBuffer.Length != dataSize)
            _tempBuffer = new byte[dataSize];

        lock (_ring)
            while (_ring.FillCount > dataSize) _ring.Read(_tempBuffer);

        wr.UpdateMesh(MemoryMarshal.Cast<byte, float>(_tempBuffer));
    }

    void OnDestroy()
    {
        if (!_ins ?.IsInvalid ?? false) _ins.Close();
        if (!_dev ?.IsInvalid ?? false) _dev.Close();
        if (!_sio ?.IsInvalid ?? false) _sio.Close();
        _self.Free();
    }

    #endregion

    #region SoundIO callback methods

    static SoundIO.InStream.ReadCallbackDelegate _readCallback = OnReadInStream;
    static SoundIO.InStream.OverflowCallbackDelegate _overflowCallback = OnOverflowInStream;
    static SoundIO.InStream.ErrorCallbackDelegate _errorCallback = OnErrorInStream;

    unsafe static void
        OnReadInStream(ref SoundIO.InStreamData stream, int frameMin, int frameMax)
    {
        var self = (Test)GCHandle.FromIntPtr(stream.UserData).Target;
        var layout = stream.Layout;

        for (var left = frameMax; left > 0;)
        {
            var frameCount = left;

            SoundIO.ChannelArea* areas;
            stream.BeginRead(out areas, ref frameCount);

            if (frameCount == 0) break;

            if (areas == null)
            {
                lock (self._ring)
                    self._ring.WriteEmpty(frameCount * stream.BytesPerFrame);
            }
            else
            {
                var span = new ReadOnlySpan<Byte>(
                    (void*)areas[0].Pointer,
                    areas[0].Step * frameCount
                );
                lock (self._ring) self._ring.Write(span);
            }

            stream.EndRead();

            left -= frameCount;
        }
    }

    static void OnOverflowInStream(ref SoundIO.InStreamData stream)
    {
        Debug.LogWarning("InStream overflow");
    }

    static void OnErrorInStream(ref SoundIO.InStreamData stream, SoundIO.Error error)
    {
        Debug.LogError($"InStream error ({error})");
    }

    #endregion

}
