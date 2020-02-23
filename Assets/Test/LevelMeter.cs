using SoundIO.SimpleDriver;
using UnityEngine;

public sealed class LevelMeter : MonoBehaviour
{
    #region Editable attributes

    [SerializeField] DeviceSelector _selector = null;
    [SerializeField, Range(0, 100)] float _amplitude = 10;

    #endregion

    #region MonoBehaviour implementation

    void Update()
    {
        var span = _selector.AudioData;

        var ss = 0.0f;
        foreach (var v in span) ss += v * v;
        var rsm = Mathf.Sqrt(ss);

        transform.localScale = new Vector3(rsm * _amplitude, 1, 1);
    }

    #endregion
}
