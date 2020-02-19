using System;
using UnityEngine;
using UnityEngine.UI;
using System.Runtime.InteropServices;

public sealed class DeviceSelector : MonoBehaviour
{
    #region Editable attributes

    [SerializeField] Dropdown _deviceList = null;
    [SerializeField] Dropdown _channelList = null;
    [SerializeField] WaveformRenderer _renderer = null;

    #endregion

    #region Internal objects

    GCHandle _self;

    SoundIO.Context _sio;
    SoundIO.Device _dev;
    SoundIO.InStream _ins;

    RingBuffer _ring = new RingBuffer(128 * 1024);
    byte[] _tempBuffer;

    bool CheckDevice(SoundIO.Device device)
    {
        return
            !device.IsRaw &&
            device.CurrentLayout.ChannelCount > 0 && // For ALSA
            !device.Name.Contains("snooping");       // For ALSA
    }

    #endregion

    #region UI Callback

    public void OnDeviceSelected(int index)
    {
        // Close the current stream and device.
        if (!_ins ?.IsInvalid ?? false) _ins.Close();
        if (!_dev ?.IsInvalid ?? false) _dev.Close();

        // New device name
        var name = _deviceList.options[index].text;

        // Find the target device.
        for (var i = 0; i < _sio.InputDeviceCount; i++)
        {
            _dev = _sio.GetInputDevice(i);
            if (!CheckDevice(_dev)) continue;
            if (string.Equals(_dev.Name, name)) break;
            _dev.Close();
        }

        if (_dev == null || _dev.IsClosed || _dev.IsInvalid)
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

        // Build the channel list.
        _channelList.ClearOptions();

        for (var i = 0; i < _dev.CurrentLayout.ChannelCount; i++)
            _channelList.options.Add(new Dropdown.OptionData() { text = $"Channel {i + 1}" });

        _channelList.RefreshShownValue();

        // Create an input stream.
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

        _deviceList.ClearOptions();

        for (var i = 0; i < _sio.InputDeviceCount; i++)
        {
            _dev = _sio.GetInputDevice(i);
            if (!CheckDevice(_dev)) continue;
            _deviceList.options.Add(new Dropdown.OptionData() { text = _dev.Name });
            _dev.Close();
        }

        if (_sio.InputDeviceCount > 0)
        {
            OnDeviceSelected(0);
            _deviceList.RefreshShownValue();
        }
    }

    void Update()
    {
        if (!_sio?.IsInvalid ?? false) _sio.FlushEvents();

        var channels = _dev.CurrentLayout.ChannelCount;
        var dataSize = _renderer.BufferSize * channels * sizeof(float);

        if (_tempBuffer == null || _tempBuffer.Length != dataSize)
            _tempBuffer = new byte[dataSize];

        lock (_ring)
            while (_ring.FillCount > dataSize) _ring.Read(_tempBuffer);

        _renderer.UpdateMesh(
            MemoryMarshal.Cast<byte, float>(_tempBuffer),
            channels, _channelList.value
        );
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

    [AOT.MonoPInvokeCallback(typeof(SoundIO.InStream.ReadCallbackDelegate))]
    unsafe static void
        OnReadInStream(ref SoundIO.InStreamData stream, int frameMin, int frameMax)
    {
        var self = (DeviceSelector)GCHandle.FromIntPtr(stream.UserData).Target;
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
