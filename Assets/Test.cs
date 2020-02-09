using UnityEngine;
using InvalidOp = System.InvalidOperationException;
using SIO = SoundIO.NativeMethods;

public sealed class Test : MonoBehaviour
{
    SIO.Context _sio;
    SIO.Device _dev;
    SIO.InStream _ins;
    SIO.RingBuffer _ring;

    unsafe static void OnReadInStream(ref SIO.InStreamData stream, int frameCountMin, int frameCountMax)
    {
        var frameLeft = frameCountMax;
        SIO.ChannelArea* areas;

        while (frameLeft > 0)
        {
            var frameCount = frameLeft;
            SIO.BeginRead(ref stream, out areas, ref frameCount);
            Debug.Log($"{frameCount} / {frameLeft}");
            if (frameCount == 0) break;
            SIO.EndRead(ref stream);
            frameLeft -= frameCount;
        }
    }

    static void OnOverflowInStream(ref SIO.InStreamData stream)
    {
    }

    static void OnErrorInStream(ref SIO.InStreamData stream, SIO.Error error)
    {
    }

    void Start()
    {
        _sio = SIO.Create();

        if (_sio?.IsInvalid ?? true)
            throw new InvalidOp("Can't create soundio context.");

        SIO.Connect(_sio);
        SIO.FlushEvents(_sio);

        _dev = SIO.GetInputDevice(_sio, SIO.DefaultInputDeviceIndex(_sio));

        if (_dev?.IsInvalid ?? true)
            throw new InvalidOp("Can't open the default input device.");
        if (_dev.Data.ProbeError != SIO.Error.None)
            throw new InvalidOp("Unable to probe device ({_dev.Data.ProbeError})");

        SIO.SortChannelLayouts(_dev);

        _ins = SIO.InStreamCreate(_dev);

        if (_ins?.IsInvalid ?? true)
            throw new InvalidOp("Can't create an input stream.");

        _ins.Data.Format = SoundIO.Format.Float32LE;
        _ins.Data.SampleRate = 48000;

        _ins.Data.Layout = SIO.ChannelLayoutGetBuiltin(SoundIO.ChannelLayoutID._7Point0);
        _ins.Data.SoftwareLatency = 0.2;

        _ins.Data.OnRead = OnReadInStream;
        _ins.Data.OnOverflow = OnOverflowInStream;
        _ins.Data.OnError = OnErrorInStream;

        var err = SIO.Open(_ins);
        if (err != SIO.Error.None)
            throw new InvalidOp($"Can't open an input stream ({err})");

        _ring = SIO.RingBufferCreate(_sio, 128 * 1024);

        SIO.Start(_ins);
    }

    void Update()
    {
        if (!_sio?.IsInvalid ?? false) SIO.FlushEvents(_sio);
    }

    void OnDestroy()
    {
        if (!_ins ?.IsInvalid ?? false) _ins.Close();
        if (!_ring?.IsInvalid ?? false) _ring.Close();
        if (!_dev ?.IsInvalid ?? false) _dev.Close();
        if (!_sio ?.IsInvalid ?? false) _sio.Close();
    }
}
