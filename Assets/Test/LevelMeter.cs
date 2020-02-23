using SoundIO.SimpleDriver;
using Unity.Mathematics;
using UnityEngine;

public sealed class LevelMeter : MonoBehaviour
{
    #region Editable attributes

    enum FilterMode { Bypass, Lowpass, Bandpass, Highpass }

    [SerializeField] DeviceSelector _selector = null;
    [SerializeField] FilterMode _filterMode = FilterMode.Bypass;
    [SerializeField] float _range = 60;
    [SerializeField] RectTransform _meter = null;

    #endregion

    #region Internal object

    BiquadFilter _filter;

    #endregion

    #region MonoBehaviour implementation

    void Update()
    {
        // Square sum
        var ss = 0.0f;

        if (_filterMode == FilterMode.Bypass)
        {
            foreach (var v in _selector.AudioData) ss += v * v;
        }
        else
        {
            var fc = 960.0f / _selector.SampleRate;

            switch (_filterMode)
            {
                case FilterMode. Lowpass: _filter. SetLowpass(fc, 0.15f); break;
                case FilterMode.Bandpass: _filter.SetBandpass(fc, 0.15f); break;
                case FilterMode.Highpass: _filter.SetHighpass(fc, 0.15f); break;
            }

            foreach (var v in _selector.AudioData)
            {
                var vf = _filter.FeedSample(v);
                ss += vf * vf;
            }
        }

        // Root mean square
        var rms = math.sqrt(ss / _selector.AudioData.Length);

        // RMS in dBFS
        // Full scale sin wave = 0 dBFS : refLevel = 1/sqrt(2)
        const float refLevel = 0.7071f;
        const float zeroOffset = 1.5849e-13f;
        var lv = 20 * math.log(rms / refLevel + zeroOffset);

        // Meter scale
        var sc = math.max(0, _range + lv) / _range;
        _meter.transform.localScale = new Vector3(sc, 1, 1);
    }

    #endregion
}
