using UnityEngine;
using InvalidOp = System.InvalidOperationException;
using SIO = SoundIO.NativeMethods;
using GCHandle = System.Runtime.InteropServices.GCHandle;
using System;

public sealed class Test : MonoBehaviour
{
    GCHandle _self;
    SIO.Context _sio;
    SIO.Device _dev;
    SIO.InStream _ins;
    SIO.RingBuffer _ring;

    unsafe static void OnReadInStream(ref SIO.InStreamData stream, int frameCountMin, int frameCountMax)
    {
        var self = (Test)GCHandle.FromIntPtr(stream.UserData).Target;

        var writePtr = SIO.WritePtr(self._ring);
        var freeBytes = SIO.FreeCount(self._ring);
        var freeCount = freeBytes / stream.BytesPerFrame;

        var writeFrames = Mathf.Min(freeCount, frameCountMax);
        var framesLeft = writeFrames;
        var layout = stream.Layout;

        while (framesLeft > 0)
        {
            var frameCount = framesLeft;

            SIO.ChannelArea* areas;
            SIO.BeginRead(ref stream, out areas, ref frameCount);

            if (frameCount == 0) break;

            if (areas == null)
            {
                new Span<Byte>((void*)writePtr, frameCount * stream.BytesPerFrame).Fill(0);
            }
            else
            {
                for (var frame = 0; frame < frameCount; frame++)
                {
                    for (var ch = 0; ch < layout.ChannelCount; ch++)
                    {
                        var len = stream.BytesPerSample;
                        var from = new Span<Byte>((void*)areas[ch].Pointer, len);
                        var to = new Span<Byte>((void*)writePtr, len);
                        from.CopyTo(to);
                        areas[ch].Pointer += areas[ch].Step;
                        writePtr += len;
                    }
                }
            }

            SIO.EndRead(ref stream);

            framesLeft -= frameCount;
        }

        var advanceBytes = writeFrames * stream.BytesPerFrame;
        SIO.AdvanceWritePtr(self._ring, advanceBytes);
    }

    static void OnOverflowInStream(ref SIO.InStreamData stream)
    {
    }

    static void OnErrorInStream(ref SIO.InStreamData stream, SIO.Error error)
    {
    }

    void Start()
    {
        _self = GCHandle.Alloc(this);

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

        _ring = SIO.RingBufferCreate(_sio, 1024 * 1024);

        _ins = SIO.InStreamCreate(_dev);

        if (_ins?.IsInvalid ?? true)
            throw new InvalidOp("Can't create an input stream.");

        _ins.Data.Format = SoundIO.Format.Float32LE;
        _ins.Data.SampleRate = 48000;

        _ins.Data.Layout = _dev.Data.Layouts[0];
        _ins.Data.SoftwareLatency = 0.2;

        _ins.Data.OnRead = OnReadInStream;
        _ins.Data.OnOverflow = OnOverflowInStream;
        _ins.Data.OnError = OnErrorInStream;
        _ins.Data.UserData = GCHandle.ToIntPtr(_self);

        var err = SIO.Open(_ins);
        if (err != SIO.Error.None)
            throw new InvalidOp($"Can't open an input stream ({err})");

        SIO.Start(_ins);
    }

    void Update()
    {
        if (!_sio?.IsInvalid ?? false) SIO.FlushEvents(_sio);

        if (!_ring?.IsInvalid ?? false)
        {
            Debug.Log(SIO.ReadPtr(_ring));
            SIO.AdvanceReadPtr(_ring, SIO.FillCount(_ring));
        }
    }

    void OnDestroy()
    {
        if (!_ins ?.IsInvalid ?? false) _ins.Close();
        if (!_ring?.IsInvalid ?? false) _ring.Close();
        if (!_dev ?.IsInvalid ?? false) _dev.Close();
        if (!_sio ?.IsInvalid ?? false) _sio.Close();
        _self.Free();
    }
}
