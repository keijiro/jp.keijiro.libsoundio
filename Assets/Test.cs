using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using GCHandle = System.Runtime.InteropServices.GCHandle;
using InvalidOp = System.InvalidOperationException;
using MemoryMarshal = System.Runtime.InteropServices.MemoryMarshal;
using SIO = SoundIO.NativeMethods;

public sealed class Test : MonoBehaviour
{
    [SerializeField] string _deviceName = "";
    [SerializeField] float _amplitude = 10;
    [SerializeField] Material _material = null;

    GCHandle _self;

    SIO.Context _sio;
    SIO.Device _dev;
    SIO.InStream _ins;

    FifoBuffer _fifo = new FifoBuffer(64 * 1024);
    Mesh _mesh;

    unsafe static void OnReadInStream
        (ref SIO.InStreamData stream, int frameMin, int frameMax)
    {
        var self = (Test)GCHandle.FromIntPtr(stream.UserData).Target;
        var layout = stream.Layout;

        for (var left = frameMax; left > 0;)
        {
            var frameCount = left;

            SIO.ChannelArea* areas;
            SIO.BeginRead(ref stream, out areas, ref frameCount);

            if (frameCount == 0) break;

            if (areas == null)
            {
                self._fifo.PushEmpty(frameCount * stream.BytesPerFrame);
            }
            else
            {
                var span = new ReadOnlySpan<Byte>(
                    (void*)areas[0].Pointer,
                    areas[0].Step * frameCount
                );
                self._fifo.Push(span);
            }

            SIO.EndRead(ref stream);

            left -= frameCount;
        }
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

        for (var i = 0; i < SIO.InputDeviceCount(_sio); i++)
        {
            _dev = SIO.GetInputDevice(_sio, i);
            if (_dev.Data.Name == _deviceName) break;
            _dev.Close();
        }

        if (_dev == null)
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
        UpdateMesh(MemoryMarshal.Cast<byte, float>(_fifo.ReadSpan));
        Graphics.DrawMesh(_mesh, transform.localToWorldMatrix, _material, gameObject.layer);
    }

    void OnDestroy()
    {
        if (!_ins ?.IsInvalid ?? false) _ins.Close();
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
