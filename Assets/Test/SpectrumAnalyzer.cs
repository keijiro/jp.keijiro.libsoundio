using SoundIO.SimpleDriver;
using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class SpectrumAnalyzer : MonoBehaviour
{
    #region Editable attributes

    [SerializeField] DeviceSelector _selector = null;
    [SerializeField] Material _material = null;

    #endregion

    #region Internal objects

    const int Resolution = 512;
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
        _dft.Analyze(_selector.Volume);

        UpdateMesh(_dft.Spectrum);

        Graphics.DrawMesh(
            _mesh, transform.localToWorldMatrix,
            _material, gameObject.layer
        );
    }

    #endregion

    #region Mesh generator

    void UpdateMesh(ReadOnlySpan<float> spectrum)
    {
        if (_mesh == null)
        {
            _mesh = new Mesh();
            _mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 10);

            using (var vertexArray = CreateVertexArray(spectrum))
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
            using (var vertexArray = CreateVertexArray(spectrum))
                _mesh.SetVertexBufferData(vertexArray, 0, 0, vertexArray.Length);
        }
    }

    NativeArray<uint> CreateIndexArray()
    {
        var buffer = new NativeArray<uint>(
            (Resolution / 2 - 1) * 2,
            Allocator.Temp, NativeArrayOptions.UninitializedMemory
        );

        var offs = 0;
        var target = 0u;

        for (var i = 0; i < Resolution / 2 - 1; i++)
        {
            buffer[offs++] = target;
            buffer[offs++] = target + 1;
            target++;
        }
        target++;

        return buffer;
    }

    NativeArray<Vector3> CreateVertexArray(ReadOnlySpan<float> spectrum)
    {
        var buffer = new NativeArray<Vector3>(
            Resolution / 2,
            Allocator.Temp, NativeArrayOptions.UninitializedMemory
        );

        var offs = 0;

        for (var vi = 0; vi < Resolution / 2; vi++)
        {
            var x = math.log10(vi) / math.log10(Resolution / 2 - 1);

            const float refLevel = 0.7071f;
            const float zeroOffset = 1.5849e-13f;
            var y = 20 * math.log10(spectrum[vi] / refLevel + zeroOffset);

            buffer[offs++] = new Vector3(x, y, 0);
        }

        return buffer;
    }

    #endregion
}
