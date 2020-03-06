using SoundIO.SimpleDriver;
using Unity.Mathematics;
using UnityEngine;

// Level meter with a low/mid/high filter bank

public sealed class LevelMeter : MonoBehaviour
{
    #region Editable attributes

    [SerializeField] DeviceSelector _selector = null;
    [SerializeField] float _range = 60;
    [SerializeField] RectTransform _bypassMeter = null;
    [SerializeField] RectTransform _lowPassMeter = null;
    [SerializeField] RectTransform _bandPassMeter = null;
    [SerializeField] RectTransform _highPassMeter = null;

    #endregion

    #region Internal object

    MultibandFilter _filter;

    #endregion

    #region MonoBehaviour implementation

    void Update()
    {
        _filter.SetParameter(960.0f / _selector.SampleRate, 0.15f);

        // Square sum
        var ss = float4.zero;

        foreach (var v in _selector.AudioData)
        {
            var vf = _filter.FeedSample(v);
            ss += vf * vf;
        }

        // Root mean square
        var rms = math.sqrt(ss / _selector.AudioData.Length);

        // RMS in dBFS
        // Full scale sin wave = 0 dBFS : refLevel = 1/sqrt(2)
        const float refLevel = 0.7071f;
        const float zeroOffset = 1.5849e-13f;
        var lv = 20 * math.log10(rms / refLevel + zeroOffset);

        // Meter scale
        var sc = math.max(0, _range + lv) / _range;

        // Apply to rect-transforms.
          _bypassMeter.transform.localScale = new Vector3(sc.x, 1, 1);
         _lowPassMeter.transform.localScale = new Vector3(sc.y, 1, 1);
        _bandPassMeter.transform.localScale = new Vector3(sc.z, 1, 1);
        _highPassMeter.transform.localScale = new Vector3(sc.w, 1, 1);
    }

    #endregion
}
