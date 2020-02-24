using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class WaveformRenderer : MonoBehaviour
{
    #region Editable attributes

    [SerializeField] DeviceSelector _selector = null;
    [SerializeField, Range(16, 1024)] int _resolution = 512;
    [SerializeField] Material _material = null;

    #endregion

    #region MonoBehaviour implementation

    void Update()
    {
        var span = _selector.AudioData;
        if (span.Length == 0) return;

        UpdateMesh(span, _selector.ChannelCount, _selector.Channel, _selector.Volume);

        Graphics.DrawMesh(
            _mesh, transform.localToWorldMatrix,
            _material, gameObject.layer
        );
    }

    void OnDestroy()
    {
        if (_mesh != null) Destroy(_mesh);
    }

    #endregion

    #region Mesh generator

    Mesh _mesh;

    void UpdateMesh(ReadOnlySpan<float> input, int stride, int offset, float amplifier)
    {
        if (_mesh == null)
        {
            _mesh = new Mesh();
            _mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 10);

            using (var vertexArray = CreateVertexArray(input, stride, offset, amplifier))
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
            using (var vertexArray = CreateVertexArray(input, stride, offset, amplifier))
                _mesh.SetVertexBufferData(vertexArray, 0, 0, vertexArray.Length);
        }
    }

    NativeArray<uint> CreateIndexArray()
    {
        var buffer = new NativeArray<uint>(
            (_resolution - 1) * 2,
            Allocator.Temp, NativeArrayOptions.UninitializedMemory
        );

        var offs = 0;
        var target = 0u;

        for (var i = 0; i < _resolution - 1; i++)
        {
            buffer[offs++] = target;
            buffer[offs++] = target + 1;
            target++;
        }

        return buffer;
    }

    NativeArray<Vector3> CreateVertexArray
        (ReadOnlySpan<float> input, int stride, int offset, float amplifier)
    {
        var buffer = new NativeArray<Vector3>(
            _resolution,
            Allocator.Temp, NativeArrayOptions.UninitializedMemory
        );

        var offs = 0;

        for (var vi = 0; vi < _resolution; vi++)
        {
            var i = (vi * (input.Length / stride) / _resolution) * stride + offset;
            var v = input[i];

            var x = (float)vi / _resolution;
            var y = v * amplifier;

            buffer[offs++] = new Vector3(x, y, 0);
        }

        return buffer;
    }

    #endregion
}
