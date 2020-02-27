using SoundIO.SimpleDriver;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Profiling;

public sealed class SpectrumAnalyzer : MonoBehaviour
{
    #region Editable attributes

    [SerializeField] DeviceSelector _selector = null;
    [SerializeField] Material _material = null;

    #endregion

    #region MonoBehaviour implementation

    void Update()
    {
        // Push the input audio data to the internal ring buffer.
        PushInput(_selector.AudioData);

        // Apply the window function to the contents of the ring buffer.
        Span<float> t_data = stackalloc float[Resolution];
        ApplyWindow(_selector.Volume, t_data);

        // Apply DFT to get the spectrum data.
        Span<float> f_data = stackalloc float[Resolution / 2];
        AnalyzeSpectrum(t_data, f_data);

        // Update the line mesh.
        UpdateMesh(f_data);

        // Draw the line mesh.
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

    #region Signal processing methods

    const int Resolution = 256;

    // Internal ring buffer
    float[] _buffer = new float[Resolution];
    int _bufferOffset;

    // Pre-calculated Blackman window function
    float[] _window =
        Enumerable.Range(0, Resolution).Select(
            n => 0.42f - 0.50f * math.cos(2 * math.PI * n / (Resolution - 1))
                       + 0.08f * math.cos(4 * math.PI * n / (Resolution - 1))
        ).ToArray();

    // Pre-calculated DFT coefficients
    float[] _dftCoeffsR =
        Enumerable.Range(0, Resolution / 2 * Resolution).Select(
            n => math.cos(2 * math.PI / Resolution * (n / Resolution) * (n % Resolution))
        ).ToArray();

    float[] _dftCoeffsI =
        Enumerable.Range(0, Resolution / 2 * Resolution).Select(
            n => math.cos(2 * math.PI / Resolution * (n / Resolution) * (n % Resolution))
        ).ToArray();

    // Push the input audio data to the internal ring buffer.
    void PushInput(ReadOnlySpan<float> input)
    {
        for (var offset = 0; offset < input.Length;)
        {
            var part = math.min(
                input.Length - offset,     // Left in the input buffer
                Resolution - _bufferOffset // Remain in the ring buffer
            );

            // Data copy
            input.Slice(offset, part).
                CopyTo(new Span<float>(_buffer, _bufferOffset, part));

            // Add the offset values.
            offset += part;
            _bufferOffset = (_bufferOffset + part) & (Resolution - 1);
        }
    }

    // Apply the window function to the contents of the ring buffer.
    void ApplyWindow(float amplifier, Span<float> output)
    {
        for (var i = 0; i < Resolution; i++)
            output[i] = _window[i] * amplifier *
                _buffer[(_bufferOffset + i) & (Resolution - 1)];
    }

    // Apply DFT to get the spectrum data.
    unsafe void AnalyzeSpectrum(ReadOnlySpan<float> input, Span<float> output)
    {
        Profiler.BeginSample("Spectrum Analyer DFT");

        fixed (
            void* I  = &input.GetPinnableReference(),
                  Cr = &_dftCoeffsR[0],
                  Ci = &_dftCoeffsI[0],
                  O  = &output.GetPinnableReference()
        )
        {
            var job = new DftJob
            {
                I  = (float4*)I,
                Cr = (float4*)Cr,
                Ci = (float4*)Ci,
                O  = (float*)O
            };
            job.Schedule(Resolution / 2, 8).Complete();
        }

        Profiler.EndSample();
    }

    [Unity.Burst.BurstCompile(CompileSynchronously = true)]
    unsafe struct DftJob : IJobParallelFor
    {
        [NativeDisableUnsafePtrRestriction] public float4* I;
        [NativeDisableUnsafePtrRestriction] public float4* Cr;
        [NativeDisableUnsafePtrRestriction] public float4* Ci;
        [NativeDisableUnsafePtrRestriction] public float* O;

        public void Execute(int i)
        {
            var rl = 0.0f;
            var im = 0.0f;

            var offs = i * Resolution / 4;

            for (var n = 0; n < Resolution / 4; n++)
            {
                var x_n = I[n];
                rl += math.dot(x_n, Cr[offs]);
                im -= math.dot(x_n, Ci[offs]);
                offs ++;
            }

            O[i] = math.sqrt(rl * rl + im * im);
        }
    }

    #endregion

    #region Mesh generator

    Mesh _mesh;

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
