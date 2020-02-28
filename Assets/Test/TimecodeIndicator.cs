using UnityEngine;
using UnityEngine.UI;

public sealed class TimecodeIndicator : MonoBehaviour
{
    [SerializeField] DeviceSelector _selector = null;
    [SerializeField] Text _label = null;

    Ltc.TimecodeDecoder _decoder = new Ltc.TimecodeDecoder();

    void Update()
    {
        _decoder.ParseAudioData(_selector.AudioData);
        var tc = _decoder.LastTimecode;
        _label.text = tc.ToString() + (tc.dropFrame ? " (drop frame)" : " (non-drop frame)");
    }
}
