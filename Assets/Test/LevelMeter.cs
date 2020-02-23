using SoundIO.SimpleDriver;
using UnityEngine;

public sealed class LevelMeter : MonoBehaviour
{
    #region Editable attributes

    enum FilterMode { Bypass, Lowpass, Bandpass, Highpass }

    [SerializeField] DeviceSelector _selector = null;
    [SerializeField] FilterMode _filterMode = FilterMode.Bypass;
    [SerializeField, Range(0, 100)] float _amplitude = 10;

    #endregion

    #region Internal object

    BiquadFilter _filter;

    #endregion

    #region MonoBehaviour implementation

    void Update()
    {
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

        transform.localScale = new Vector3(Mathf.Sqrt(ss) * _amplitude, 1, 1);
    }

    #endregion
}
