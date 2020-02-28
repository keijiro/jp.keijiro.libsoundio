using System;
using UnityEngine;
using UnityEngine.UI;

public struct TimeCode
{
    public int frame;
    public int second;
    public int minute;
    public int hour;

    static public TimeCode Decode(ulong data)
    {
        var s1 = (int)((data      ) & 0xffff);
        var s2 = (int)((data >> 16) & 0xffff);
        var s3 = (int)((data >> 32) & 0xffff);
        var s4 = (int)((data >> 48) & 0xffff);

        return new TimeCode
        {
            frame  = (s1 & 0xf) + ((s1 >> 8) & 3) * 10,
            second = (s2 & 0xf) + ((s2 >> 8) & 7) * 10,
            minute = (s3 & 0xf) + ((s3 >> 8) & 7) * 10,
            hour   = (s4 & 0xf) + ((s4 >> 8) & 3) * 10
        };
    }

    public override string ToString()
    {
        return $"({hour:D2}:{minute:D2}:{second:D2}:{frame:D2})";
    }
}

public sealed class TimeCodeDecoder
{
    #region Public methods

    public TimeCode LastTimeCode { get; private set; }

    public void ParseAudioData(ReadOnlySpan<float> data)
    {
        foreach (var v in data) ProcessSample(v > 0.0f);
    }

    #endregion

    #region Internal state

    (ulong lo, ulong hi) _buffer;

    int _count;
    int _interval;
    bool _tick;
    bool _state;

    #endregion

    #region Private methods

    void ProcessSample(bool flag)
    {
        if (_state == flag)
        {
            if (_count < 10000) _count++;
            return;
        }

        if (_count < _interval / 100)
        {
            if (_tick)
            {
                ProcessBit(true);
                _tick = false;
            }
            else
                _tick = true;
        }
        else
        {
            _tick = false;
            ProcessBit(false);
        }

        _interval = (_interval * 99 + _count * 100) / 100;
        _state = flag;
        _count = 0;
    }

    void ProcessBit(bool bit)
    {
        const ulong msb64 = 1ul << 63;
        const ushort sync = 0xbffc;

        var hi_lsb = (_buffer.hi & 1ul) != 0;

        _buffer.lo = (_buffer.lo >> 1) | (hi_lsb ? msb64 : 0ul);
        _buffer.hi = (_buffer.hi >> 1) | (   bit ? msb64 : 0ul);

        if ((ushort)_buffer.hi == sync)
            LastTimeCode = TimeCode.Decode(_buffer.lo);
    }

    #endregion
}

public sealed class LtcDecoder : MonoBehaviour
{
    [SerializeField] DeviceSelector _selector = null;
    [SerializeField] Text _label = null;

    TimeCodeDecoder _decoder = new TimeCodeDecoder();

    void Update()
    {
        _decoder.ParseAudioData(_selector.AudioData);
        _label.text = _decoder.LastTimeCode.ToString();
    }
}
