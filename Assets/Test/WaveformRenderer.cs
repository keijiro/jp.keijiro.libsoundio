using System;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

// Raw waveform renderer

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
        if (_selector.AudioData.Length == 0) return;

        UpdateMesh();

        Graphics.DrawMesh(
            _mesh, transform.localToWorldMatrix,
            _material, gameObject.layer);
    }

    void OnDestroy()
    {
        if (_mesh != null) Destroy(_mesh);
    }

    #endregion

    #region Mesh generator

    Mesh _mesh;

    void UpdateMesh()
    {
        if (_mesh == null)
        {
            _mesh = new Mesh();
            _mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 10);

            // Initial vertices
            using (var vertices = CreateVertexArray())
            {
                var pos = new VertexAttributeDescriptor(
                    VertexAttribute.Position,
                    VertexAttributeFormat.Float32, 3);

                _mesh.SetVertexBufferParams(vertices.Length, pos);
                _mesh.SetVertexBufferData(vertices, 0, 0, vertices.Length);
            }

            // Initial indices
            using (var indices = CreateIndexArray())
            {
                _mesh.SetIndexBufferParams(indices.Length, IndexFormat.UInt32);
                _mesh.SetIndexBufferData(indices, 0, 0, indices.Length);

                var lines = new SubMeshDescriptor
                    (0, indices.Length, MeshTopology.LineStrip);

                _mesh.SetSubMesh(0, lines);
            }
        }
        else
        {
            // Vertex update
            using (var vertices = CreateVertexArray())
                _mesh.SetVertexBufferData(vertices, 0, 0, vertices.Length);
        }
    }

    NativeArray<int> CreateIndexArray()
    {
        var indices = Enumerable.Range(0, _resolution);
        return new NativeArray<int>(indices.ToArray(), Allocator.Temp);
    }

    NativeArray<Vector3> CreateVertexArray()
    {
        var data = _selector.AudioData;

        var buffer = new NativeArray<Vector3>(
            _resolution, Allocator.Temp,
            NativeArrayOptions.UninitializedMemory);

        for (var vi = 0; vi < _resolution; vi++)
        {
            var x = (float)vi / _resolution;
            var i = vi * data.Length / _resolution;
            buffer[vi] = new Vector3(x, data[i], 0);
        }

        return buffer;
    }

    #endregion
}
