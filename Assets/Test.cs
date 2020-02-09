using UnityEngine;
using IntPtr = System.IntPtr;
using InvalidOp = System.InvalidOperationException;
using Marshal = System.Runtime.InteropServices.Marshal;
using SIO = SoundIO.Unmanaged;
using Unsafe = System.Runtime.CompilerServices.Unsafe;

public unsafe sealed class Test : MonoBehaviour
{
    static int _counter;

    SIO.Context* p_sio;
    SIO.Device* p_dev;
    SIO.InStream* p_ins;
    SIO.RingBuffer* p_ring;

    static void OnReadInStream(ref SIO.InStream stream, int frameCountMin, int frameCountMax)
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

            _counter++;
        }
    }

    static void OnOverflowInStream(ref SIO.InStream stream)
    {
    }

    static void OnErrorInStream(ref SIO.InStream stream, SIO.Error error)
    {
    }

    unsafe void Start()
    {
        try
        {
            p_sio = SIO.Create();
            if (p_sio == null)
                throw new InvalidOp("Can't create soundio context.");

            ref var sio = ref Unsafe.AsRef<SIO.Context>(p_sio);

            SIO.Connect(ref sio);
            SIO.FlushEvents(ref sio);

            p_dev = SIO.GetInputDevice(ref sio, SIO.DefaultInputDeviceIndex(ref sio));
            if (p_dev == null)
                throw new InvalidOp("Can't open the default input device.");

            ref var dev = ref Unsafe.AsRef<SIO.Device>(p_dev);

            if (dev.ProbeError != SIO.Error.None)
                throw new InvalidOp("Unable to probe device ({dev.ProbeError})");

            SIO.SortChannelLayouts(ref dev);

            p_ins = SIO.InStreamCreate(ref dev);
            if (p_ins == null)
                throw new InvalidOp("Can't create an input stream.");

            ref var ins = ref Unsafe.AsRef<SIO.InStream>(p_ins);

            ins.Format = SoundIO.Format.Float32LE;
            ins.SampleRate = 48000;

            ins.Layout = SIO.ChannelLayoutGetBuiltin(SoundIO.ChannelLayoutID._7Point0);
            ins.SoftwareLatency = 0.2;

            ins.OnRead = OnReadInStream;
            ins.OnOverflow = OnOverflowInStream;
            ins.OnError = OnErrorInStream;

            var err = SIO.Open(ref ins);
            if (err != SIO.Error.None)
                throw new InvalidOp($"Can't open an input stream ({err})");

            p_ring = SIO.RingBufferCreate(ref sio, 4 * 1024 * 1024);

            SIO.Start(ref ins);
        }
        catch (InvalidOp e)
        {
            Debug.LogError(e);
        }
        finally
        {
        }
    }

    void Update()
    {
        ref var sio = ref Unsafe.AsRef<SIO.Context>(p_sio);
        SIO.FlushEvents(ref sio);
        //Debug.Log($"End {_counter}");
    }

    void OnDestroy()
    {
        if (p_ins != null) SIO.Destroy(p_ins);
        if (p_ring != null) SIO.Destroy(p_ring);
        if (p_dev != null) SIO.Unref(p_dev);
        if (p_sio != null) SIO.Destroy(p_sio);
    }
}
