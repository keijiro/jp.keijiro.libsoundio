using UnityEngine;
using IntPtr = System.IntPtr;
using InvalidOp = System.InvalidOperationException;
using Marshal = System.Runtime.InteropServices.Marshal;
using SIO = SoundIO.Unmanaged;

public unsafe sealed class Test : MonoBehaviour
{
    static int _counter;

    SIO.Context* sio;
    SIO.Device* dev;
    SIO.InStream* ins;
    SIO.RingBuffer* ring;

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
            sio = SIO.Create();
            if (sio == null)
                throw new InvalidOp("Can't create soundio context.");

            SIO.Connect(sio);
            SIO.FlushEvents(sio);

            dev = SIO.GetInputDevice(sio, SIO.DefaultInputDeviceIndex(sio));
            if (dev == null)
                throw new InvalidOp("Can't open the default input device.");

            if (dev->ProbeError != SIO.Error.None)
                throw new InvalidOp("Unable to probe device ({dev->ProbeError})");

            SIO.SortChannelLayouts(dev);

            ins = SIO.InStreamCreate(dev);
            if (ins == null)
                throw new InvalidOp("Can't create an input stream.");

            ins->Format = SoundIO.Format.Float32LE;
            ins->SampleRate = 48000;

            ins->Layout = *SIO.ChannelLayoutGetBuiltin(SoundIO.ChannelLayoutID._7Point0);
            ins->SoftwareLatency = 0.2;

            ins->OnRead = OnReadInStream;
            ins->OnOverflow = OnOverflowInStream;
            ins->OnError = OnErrorInStream;

            var err = SIO.Open(ins);
            if (err != SIO.Error.None)
                throw new InvalidOp($"Can't open an input stream ({err})");

            //ring = SIO.RingBufferCreate(sio, 4 * 1024 * 1024);

            SIO.Start(ins);
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
        SIO.FlushEvents(sio);
        //Debug.Log($"End {_counter}");
    }

    void OnDestroy()
    {
        if (ins != null) SIO.Destroy(ins);
        if (ring != null) SIO.Destroy(ring);
        if (dev != null) SIO.Unref(dev);
        if (sio != null) SIO.Destroy(sio);
    }
}
