using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;
using InvalidOp = System.InvalidOperationException;
using SIO = SoundIO.NativeMethods;
using GCHandle = System.Runtime.InteropServices.GCHandle;
using System;

public sealed class Test : MonoBehaviour
{
    [SerializeField] float _amplitude = 10;
    [SerializeField] Material _material = null;


    GCHandle _self;
    SIO.Context _sio;
    SIO.Device _dev;
    SIO.InStream _ins;
    SIO.RingBuffer _ring;

    Mesh _mesh;

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
        Debug.Log($"InStream error ({error})");
    }

    static SIO.InStreamData.ReadCallback _onReadInStream = OnReadInStream;
    static SIO.InStreamData.OverflowCallback _onOverflowInStream = OnOverflowInStream;
    static SIO.InStreamData.ErrorCallback _onErrorInStream = OnErrorInStream;

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
        _ins.Data.SoftwareLatency = 1.0 / 60;

        _ins.Data.OnRead = _onReadInStream;
        _ins.Data.OnOverflow = _onOverflowInStream;
        _ins.Data.OnError = _onErrorInStream;
        _ins.Data.UserData = GCHandle.ToIntPtr(_self);

        var err = SIO.Open(_ins);
        if (err != SIO.Error.None)
            throw new InvalidOp($"Can't open an input stream ({err})");

        SIO.Start(_ins);
    }

    unsafe void Update()
    {
        if (!_sio?.IsInvalid ?? false) SIO.FlushEvents(_sio);

        if (!_ring?.IsInvalid ?? false)
        {
            var fill = SIO.FillCount(_ring);

            if (fill > 0)
            {
                var buffer = new ReadOnlySpan<float>(
                    (void*)SIO.ReadPtr(_ring),
                    fill / 4
                );

                UpdateMesh(buffer);

                SIO.AdvanceReadPtr(_ring, fill);
            }
        }

        Graphics.DrawMesh(_mesh, transform.localToWorldMatrix, _material, gameObject.layer);
    }

    void OnDestroy()
    {
        if (!_ins ?.IsInvalid ?? false) _ins.Close();
        if (!_ring?.IsInvalid ?? false) _ring.Close();
        if (!_dev ?.IsInvalid ?? false) _dev.Close();
        if (!_sio ?.IsInvalid ?? false) _sio.Close();
        _self.Free();

        if (_mesh != null) Destroy(_mesh);
    }

    #region Line mesh generator

    const int Resolution = 480;
    const int Channels = 7;

    void UpdateMesh(ReadOnlySpan<float> input)
    {
        if (_mesh == null)
        {
            _mesh = new Mesh();

            using (var vertexArray = CreateVertexArray(input))
            {
                _mesh.SetVertexBufferParams(
                    vertexArray.Length,
                    new VertexAttributeDescriptor
                        (VertexAttribute.Position, VertexAttributeFormat.Float32, 3)
                );
                _mesh.SetVertexBufferData(vertexArray, 0, 0, vertexArray.Length);
            }

            using (var indexArray = CreateIndexArray())
            {
                _mesh.SetIndexBufferParams(indexArray.Length, IndexFormat.UInt32);
                _mesh.SetIndexBufferData(indexArray, 0, 0, indexArray.Length);
                _mesh.SetSubMesh(0, new SubMeshDescriptor(0, indexArray.Length, MeshTopology.Lines));
            }
        }
        else
        {
            using (var vertexArray = CreateVertexArray(input))
                _mesh.SetVertexBufferData(vertexArray, 0, 0, vertexArray.Length);
        }
    }

    NativeArray<uint> CreateIndexArray()
    {
        var buffer = new NativeArray<uint>(
            (Resolution - 1) * 2 * Channels,
            Allocator.Temp, NativeArrayOptions.UninitializedMemory
        );

        var offs = 0;
        var target = 0u;

        for (var ch = 0; ch < Channels; ch++)
        {
            for (var i = 0; i < Resolution - 1; i++)
            {
                buffer[offs++] = target;
                buffer[offs++] = target + 1;
                target++;
            }
            target++;
        }

        return buffer;
    }

    NativeArray<Vector3> CreateVertexArray(ReadOnlySpan<float> input)
    {
        var buffer = new NativeArray<Vector3>(
            Resolution * Channels,
            Allocator.Temp, NativeArrayOptions.UninitializedMemory
        );

        var offs = 0;

        for (var ch = 0; ch < Channels; ch++)
        {
            for (var vi = 0; vi < Resolution; vi++)
            {
                var i = vi * Channels + ch;
                var v = i < input.Length ? input[i] : 0;

                var x = (float)vi / Resolution;
                var y = ch + v * _amplitude;

                buffer[offs++] = new Vector3(x, y, 0);
            }
        }

        return buffer;
    }

    #endregion
}
