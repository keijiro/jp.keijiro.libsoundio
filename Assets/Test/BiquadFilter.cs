using Unity.Mathematics;

// A simple implementation of a biquad IIR filter
// based on the EarLevel Engineering blog article.
// http://www.earlevel.com/main/2012/11/26/biquad-c-source-code/

public struct BiquadFilter
{
    float _a0, _a1, _a2, _b1, _b2;
    float _z1, _z2;

    public void SetLowpass(float Fc, float Q)
    {
        var K = math.tan((float)math.PI * Fc);
        var norm = 1 / (1 + K / Q + K * K);
        _a0 = K * K * norm;
        _a1 = 2 * _a0;
        _a2 = _a0;
        _b1 = 2 * (K * K - 1) * norm;
        _b2 = (1 - K / Q + K * K) * norm;
    }

    public void SetBandpass(float Fc, float Q)
    {
        var K = math.tan((float)math.PI * Fc);
        var norm = 1 / (1 + K / Q + K * K);
        _a0 = K / Q * norm;
        _a1 = 0;
        _a2 = -_a0;
        _b1 = 2 * (K * K - 1) * norm;
        _b2 = (1 - K / Q + K * K) * norm;
    }

    public void SetHighpass(float Fc, float Q)
    {
        var K = math.tan((float)math.PI * Fc);
        var norm = 1 / (1 + K / Q + K * K);
        _a0 = norm;
        _a1 = -2 * _a0;
        _a2 = _a0;
        _b1 = 2 * (K * K - 1) * norm;
        _b2 = (1 - K / Q + K * K) * norm;
    }

    public float FeedSample(float i)
    {
        var o = i * _a0 + _z1;
        _z1 = i * _a1 + _z2 - _b1 * o;
        _z2 = i * _a2 - _b2 * o;
        return o;
    }
}
