using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace UnitySioTest
{
    public sealed class DeviceSelector : MonoBehaviour
    {
        [SerializeField] SoundIODriver _driver = null;
        [SerializeField] Dropdown _deviceList = null;
        [SerializeField] Dropdown _channelList = null;
        [SerializeField] Text _statusText = null;

        public int Channel => _channelList.value;

        void Start()
        {
            // Device list initialization
            _deviceList.ClearOptions();
            _deviceList.options = _driver.GetDeviceNameList().
                Select(name => new Dropdown.OptionData(){ text = name }).ToList();

            // Select the first device.
            if (_driver.DeviceCount > 0) OnDeviceSelected(0);

            _deviceList.RefreshShownValue();
        }

        public void OnDeviceSelected(int index)
        {
            _driver.SelectDevice(index);

            // Channel list (re)initialization
            _channelList.ClearOptions();

            _channelList.options = Enumerable.Range(0, _driver.ChannelCount).
                Select(i => new Dropdown.OptionData(){ text = $"Channel {i + 1}" }).ToList();

            _channelList.RefreshShownValue();

            _statusText.text =
                $"{_driver.SampleRate} Hz {_driver.Latency * 1000} ms software latency";
        }
    }
}
