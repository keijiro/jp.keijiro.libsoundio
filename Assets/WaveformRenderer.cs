using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class WaveformRenderer : MonoBehaviour
{
    #region Constants

    const int ChannelCount = 7;

    #endregion

    #region Editable attributes

    [SerializeField, Range(16, 1024)] int _resolution = 512;
    [SerializeField, Range(0, 100)] float _amplitude = 10;
    [SerializeField] Material _material = null;

    #endregion

    #region Internal objects

    Mesh _mesh;

    #endregion

    #region Public property and method

    public int BufferSize => _resolution * ChannelCount;

    public void UpdateMesh(ReadOnlySpan<float> input)
    {
        Debug.Assert(input.Length == BufferSize);

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

    #endregion

    #region MonoBehaviour implementation

    void Update()
    {
        if (_mesh != null)
            Graphics.DrawMesh(_mesh, transform.localToWorldMatrix, _material, gameObject.layer);
    }

    void OnDestroy()
    {
        if (_mesh != null) Destroy(_mesh);
    }

    #endregion

    #region Mesh reconstruction

    NativeArray<uint> CreateIndexArray()
    {
        var buffer = new NativeArray<uint>(
            (_resolution - 1) * 2 * ChannelCount,
            Allocator.Temp, NativeArrayOptions.UninitializedMemory
        );

        var offs = 0;
        var target = 0u;

        for (var ch = 0; ch < ChannelCount; ch++)
        {
            for (var i = 0; i < _resolution - 1; i++)
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
            _resolution * ChannelCount,
            Allocator.Temp, NativeArrayOptions.UninitializedMemory
        );

        var offs = 0;

        for (var ch = 0; ch < ChannelCount; ch++)
        {
            for (var vi = 0; vi < _resolution; vi++)
            {
                var i = vi * ChannelCount + ch;
                var v = i < input.Length ? input[i] : 0;

                var x = (float)vi / _resolution;
                var y = ch + v * _amplitude;

                buffer[offs++] = new Vector3(x, y, 0);
            }
        }

        return buffer;
    }

    #endregion
}
