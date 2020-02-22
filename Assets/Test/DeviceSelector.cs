using SoundIO.SimpleDriver;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public sealed class DeviceSelector : MonoBehaviour
{
    [SerializeField] Dropdown _deviceList = null;
    [SerializeField] Dropdown _channelList = null;
    [SerializeField] Text _statusText = null;
    [SerializeField] WaveformRenderer _renderer = null;

    InputStream _stream;

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

    public void OnDeviceSelected(int index)
    {
        // Reset the current state.
        _renderer.Stream = null;

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

        _renderer.Stream = _stream;

        // Construct the channel list.
        _channelList.options = Enumerable.Range(0, _stream.ChannelCount).
            Select(i => new Dropdown.OptionData(){ text = $"Channel {i + 1}" }).ToList();

        _channelList.RefreshShownValue();

        // Status line
        _statusText.text =
            $"{_stream.SampleRate} Hz {_stream.Latency * 1000} ms software latency";
    }
}
