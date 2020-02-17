using System;
using UnityEngine;
using GCHandle = System.Runtime.InteropServices.GCHandle;
using MemoryMarshal = System.Runtime.InteropServices.MemoryMarshal;
using SIO = SoundIO.NativeMethods;

public sealed class Test : MonoBehaviour
{
    #region Editable attributes

    [SerializeField] string _deviceName = "";

    #endregion

    #region Internal objects

    GCHandle _self;

    SIO.Context _sio;
    SIO.Device _dev;
    SIO.InStream _ins;

    RingBuffer _ring = new RingBuffer(128 * 1024);
    byte[] _tempBuffer;

    #endregion

    #region MonoBehaviour implementation

    void Start()
    {
        _self = GCHandle.Alloc(this);

        _sio = SIO.Create();

        if (_sio?.IsInvalid ?? true)
        {
            Debug.LogError("Can't create soundio context.");
            return;
        }

        SIO.Connect(_sio);
        SIO.FlushEvents(_sio);

        for (var i = 0; i < SIO.InputDeviceCount(_sio); i++)
        {
            _dev = SIO.GetInputDevice(_sio, i);
            if (_dev.Data.Name == _deviceName) break;
            _dev.Close();
        }

        if (_dev.IsClosed)
            _dev = SIO.GetInputDevice(_sio, SIO.DefaultInputDeviceIndex(_sio));

        if (_dev?.IsInvalid ?? true)
        {
            Debug.LogError("Can't open the default input device.");
            return;
        }

        if (_dev.Data.ProbeError != SIO.Error.None)
        {
            Debug.LogError("Unable to probe device ({_dev.Data.ProbeError})");
            return;
        }

        SIO.SortChannelLayouts(_dev);

        _ins = SIO.InStreamCreate(_dev);

        if (_ins?.IsInvalid ?? true)
        {
            Debug.LogError("Can't create an input stream.");
            return;
        }

        _ins.Data.Format = SoundIO.Format.Float32LE;
        _ins.Data.SoftwareLatency = 1.0 / 60;
        _ins.Data.OnRead = _onReadInStream;
        _ins.Data.OnOverflow = _onOverflowInStream;
        _ins.Data.OnError = _onErrorInStream;
        _ins.Data.UserData = GCHandle.ToIntPtr(_self);

        var err = SIO.Open(_ins);

        if (err != SIO.Error.None)
        {
            Debug.LogError($"Can't open an input stream ({err})");
            return;
        }

        SIO.Start(_ins);
    }

    void Update()
    {
        if (!_sio?.IsInvalid ?? false) SIO.FlushEvents(_sio);

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

    static SIO.InStreamData.ReadCallback _onReadInStream = OnReadInStream;
    static SIO.InStreamData.OverflowCallback _onOverflowInStream = OnOverflowInStream;
    static SIO.InStreamData.ErrorCallback _onErrorInStream = OnErrorInStream;

    unsafe static void
        OnReadInStream(ref SIO.InStreamData stream, int frameMin, int frameMax)
    {
        var self = (Test)GCHandle.FromIntPtr(stream.UserData).Target;
        var layout = stream.Layout;

        for (var left = frameMax; left > 0;)
        {
            var frameCount = left;

            SIO.ChannelArea* areas;
            SIO.BeginRead(ref stream, out areas, ref frameCount);

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

            SIO.EndRead(ref stream);

            left -= frameCount;
        }
    }

    static void OnOverflowInStream(ref SIO.InStreamData stream)
    {
        Debug.LogWarning("InStream overflow");
    }

    static void OnErrorInStream(ref SIO.InStreamData stream, SIO.Error error)
    {
        Debug.LogError($"InStream error ({error})");
    }

    #endregion

}
