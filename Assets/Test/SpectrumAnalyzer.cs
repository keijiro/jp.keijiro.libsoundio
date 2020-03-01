using System;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

// DFT spectrum analyzer graph

public sealed class SpectrumAnalyzer : MonoBehaviour
{
    #region Editable attributes

    [SerializeField] DeviceSelector _selector = null;
    [SerializeField] Material _material = null;

    #endregion

    #region Internal objects

    const int Resolution = 1024;
    DftBuffer _dft;
    Mesh _mesh;

    #endregion

    #region MonoBehaviour implementation

    void Start()
    {
        _dft = new DftBuffer(Resolution);
    }

    void OnDestroy()
    {
        _dft?.Dispose();
        if (_mesh != null) Destroy(_mesh);
    }

    void Update()
    {
        _dft.Push(_selector.AudioData);
        _dft.Analyze();

        UpdateMesh();

        Graphics.DrawMesh(
            _mesh, transform.localToWorldMatrix,
            _material, gameObject.layer);
    }

    #endregion

    #region Mesh generator

    void UpdateMesh()
    {
        if (_mesh == null)
        {
            _mesh = new Mesh();
            _mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 1000);

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

                var lines = new SubMeshDescriptor(
                    0, indices.Length, MeshTopology.LineStrip);

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
        var indices = Enumerable.Range(0, Resolution / 2);
        return new NativeArray<int>(indices.ToArray(), Allocator.Temp);
    }

    NativeArray<Vector3> CreateVertexArray()
    {
        var data = _dft.Spectrum;

        var buffer = new NativeArray<Vector3>(
            Resolution / 2, Allocator.Temp,
            NativeArrayOptions.UninitializedMemory);

        const float refLevel = 0.7071f;
        const float zeroOffset = 1.5849e-13f;

        for (var vi = 0; vi < Resolution / 2; vi++)
        {
            var x = Mathf.Log10(vi) / Mathf.Log10(Resolution / 2 - 1);
            var y = 20 * Mathf.Log10(data[vi] / refLevel + zeroOffset);
            buffer[vi] = new Vector3(x, y, 0);
        }

        return buffer;
    }

    #endregion
}
