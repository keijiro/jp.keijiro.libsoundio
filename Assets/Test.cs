using UnityEngine;
using IntPtr = System.IntPtr;
using InvalidOp = System.InvalidOperationException;
using Marshal = System.Runtime.InteropServices.Marshal;
using SIO = SoundIO.Unmanaged;

public sealed class Test : MonoBehaviour
{
    static int _counter;

    unsafe void ReadCallback(SIO.InStream* stream, int frameCountMin, int frameCountMax)
    {
        _counter++;
    }

    unsafe void OverflowCallback(SIO.InStream* stream)
    {
    }

    unsafe void ErrorCallback(SIO.InStream* stream, SIO.Error error)
    {
    }

    unsafe void Start()
    {
        SIO.Instance* sio = null;
        SIO.Device* dev = null;
        SIO.InStream* ins = null;
        SIO.RingBuffer* ring = null;

        try
        {
            sio = SIO.Create();
            if (sio == null)
                throw new InvalidOp("Can't create soundio instance.");

            SIO.Connect(sio);
            SIO.FlushEvents(sio);

            dev = SIO.GetInputDevice(sio, SIO.DefaultInputDeviceIndex(sio));
            if (dev == null)
                throw new InvalidOp("Can't open the default input device.");

            if (dev->probeError != SIO.Error.None)
                throw new InvalidOp("Unable to probe device ({dev->probeError})");

            SIO.SortChannelLayouts(dev);

            ins = SIO.InStreamCreate(dev);
            if (ins == null)
                throw new InvalidOp("Can't create an input stream.");

            ins->format = SoundIO.Format.Float32LE;
            ins->sampleRate = 48000;

            ins->layout = *SIO.ChannelLayoutGetBuiltin(SoundIO.ChannelLayoutID._7Point0);
            ins->softwareLatency = 0.2;

            ins->readCallback = Marshal.GetFunctionPointerForDelegate(new SIO.ReadCallback(ReadCallback));
            ins->overflowCallback = Marshal.GetFunctionPointerForDelegate(new SIO.OverflowCallback(OverflowCallback));
            ins->errorCallback = Marshal.GetFunctionPointerForDelegate(new SIO.ErrorCallback(ErrorCallback));
            ins->userData = null;

            var err = SIO.Open(ins);
            if (err != SIO.Error.None)
                throw new InvalidOp($"Can't open an input stream ({err})");

            ring = SIO.RingBufferCreate(sio, 4 * 1024 * 1024);

            SIO.Start(ins);

            Debug.Log("Start");

            System.Threading.Thread.Sleep(3000);

            Debug.Log($"End {_counter}");
        }
        catch (InvalidOp e)
        {
            Debug.LogError(e);
        }
        finally
        {
            if (ins != null) SIO.Destroy(ins);
            if (ring != null) SIO.Destroy(ring);
            if (dev != null) SIO.UnrefDevice(dev);
            if (sio != null) SIO.Destroy(sio);
        }
    }
}
