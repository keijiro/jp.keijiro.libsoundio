using SoundIO.SimpleDriver;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

public sealed class DeviceSelector : MonoBehaviour
{
    #region Editable attributes

    [SerializeField] Dropdown _deviceList = null;
    [SerializeField] Dropdown _channelList = null;
    [SerializeField] Text _statusText = null;

    #endregion

    #region Public properties

    public int Channel => _channelList.value;
    public int ChannelCount => _stream?.ChannelCount ?? 0;
    public int SampleRate => _stream?.SampleRate ?? 0;

    public float Volume { get; set; } = 1;

    public ReadOnlySpan<float> AudioData =>
        _stream == null ? ReadOnlySpan<float>.Empty :
            MemoryMarshal.Cast<byte, float>(_stream.LastFrameWindow);

    #endregion

    #region Internal objects

    InputStream _stream;

    #endregion

    #region MonoBehaviour implementation

    void Start()
    {
        // Clear the UI contents.
        _deviceList.ClearOptions();
        _channelList.ClearOptions();
        _statusText.text = "";

        // Device list initialization
        _deviceList.options.Add(new Dropdown.OptionData(){ text = "--" });

        for (var i = 0; i < DeviceDriver.DeviceCount; i++)
            _deviceList.options.Add(
                new Dropdown.OptionData()
                    { text = DeviceDriver.GetDeviceName(i) }
            );

        _deviceList.RefreshShownValue();
    }

    void OnDestroy()
    {
        _stream?.Dispose();
    }

    void Update()
    {
        if (_stream == null) return;

        // Status line
        _statusText.text =
            $"Sampling rate: {_stream.SampleRate:n0}Hz / " +
            $"Software Latency: {_stream.Latency * 1000:n2}ms / " +
            $"Amplifier: {Volume:P0}";
    }

    #endregion

    #region UI callback

    public void OnDeviceSelected(int index)
    {
        // Reset the current state.
        if (_stream != null)
        {
            _stream.Dispose();
            _stream = null;
        }

        _channelList.ClearOptions();
        _channelList.RefreshShownValue();

        _statusText.text = "";

        // Break if no selection.
        if (_deviceList.value == 0) return;

        // Open a new stream.
        try
        {
            _stream = DeviceDriver.OpenInputStream(_deviceList.value - 1);
        }
        catch (System.InvalidOperationException e)
        {
            _statusText.text = $"Error: {e.Message}";
            return;
        }

        // Construct the channel list.
        _channelList.options = Enumerable.Range(0, _stream.ChannelCount).
            Select(i => new Dropdown.OptionData(){ text = $"Channel {i + 1}" }).ToList();

        _channelList.RefreshShownValue();
    }

    #endregion
}
