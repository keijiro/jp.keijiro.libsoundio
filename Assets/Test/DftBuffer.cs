using System;
using System.Linq;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Profiling;

public sealed class DftBuffer
{
    public int Width { get; private set; }

    public DftBuffer(int width)
    {
        Width = width;

        // Blackman window
        var window = Enumerable.Range(0, Width).
            Select(n => 2 * math.PI * n / (Width - 1)).
            Select(x => 0.42f - 0.5f * math.cos(x) + 0.08f * math.cos(2 * x));

        // DFT coefficients
        var coeffsR = Enumerable.Range(0, Width / 2 * Width).
            Select(i => (k: i / Width, n: i % Width)).
            Select(I => math.cos(2 * math.PI / Width * I.k * I.n));

        var coeffsI = Enumerable.Range(0, Width / 2 * Width).
            Select(i => (k: i / Width, n: i % Width)).
            Select(I => math.sin(2 * math.PI / Width * I.k * I.n));

        // Allocation and initialization
        _buffer = (new float[Width], 0);
        _window = window.ToArray();
        _coeffsR = coeffsR.ToArray();
        _coeffsI = coeffsI.ToArray();
    }

    // Push audio data to the internal ring buffer.
    public void Push(ReadOnlySpan<float> input)
    {
        for (var offset = 0; offset < input.Length;)
        {
            var part = math.min(
                input.Length - offset, // Left in the data buffer
                Width - _buffer.offset // Remain in the ring buffer
            );

            // Data copy
            input.Slice(offset, part).
                CopyTo(new Span<float>(_buffer.array, _buffer.offset, part));

            // Add the offset values.
            offset += part;
            _buffer.offset = (_buffer.offset + part) & (Width - 1);
        }
    }

    public unsafe void Analyze(float amp, Span<float> output)
    {
        // Apply the window function to the contents of the ring buffer.
        Span<float> data = stackalloc float[Width];

        for (var i = 0; i < Width; i++)
            data[i] = _window[i] * amp *
                _buffer.array[(_buffer.offset + i) & (Width - 1)];

        // Apply DFT to get the spectrum data.
        Profiler.BeginSample("Spectrum Analyer DFT");

        fixed (
            void* I  = &data.GetPinnableReference(),
                  Cr = &_coeffsR[0],
                  Ci = &_coeffsI[0],
                  O  = &output.GetPinnableReference()
        )
        {
            var job = new DftJob
            {
                input   = (float4*)I,
                coeffsR = (float4*)Cr,
                coeffsI = (float4*)Ci,
                output  = (float*)O,
                steps   = Width / 4
            };
            job.Schedule(Width / 2, 4).Complete();
        }

        Profiler.EndSample();
    }

    // Input ring buffer
    (float[] array, int offset) _buffer;

    // Pre-calculated coefficients
    float[] _window;
    float[] _coeffsR;
    float[] _coeffsI;

    #region Window function job

    /*
    [Unity.Burst.BurstCompile(CompileSynchronously = true)]
    unsafe struct WindowJob : IJobFor
    {
        // We dare to use raw pointers without safety.
        [NativeDisableUnsafePtrRestriction] public float4* input;
        [NativeDisableUnsafePtrRestriction] public float4* window;
        [NativeDisableUnsafePtrRestriction] public float4* output;

        public int inputLength;
        public int inputOffset;
        public float amplifier;

        public void Execute(int i)
        {
            output[i] = _window[i] * amplifier *
                input[(inputOffset + i) & (inputLength - 1)];
        }
    }
    */

    #endregion

    #region DFT kernel job

    [Unity.Burst.BurstCompile(CompileSynchronously = true)]
    unsafe struct DftJob : IJobParallelFor
    {
        // We dare to use raw pointers without safety.
        [NativeDisableUnsafePtrRestriction] public float4* input;
        [NativeDisableUnsafePtrRestriction] public float4* coeffsR;
        [NativeDisableUnsafePtrRestriction] public float4* coeffsI;
        [NativeDisableUnsafePtrRestriction] public float* output;
        public int steps;

        public void Execute(int i)
        {
            var offs = i * steps;

            var rl = 0.0f;
            var im = 0.0f;

            for (var n = 0; n < steps; n++)
            {
                var x_n = input[n];
                rl += math.dot(x_n, coeffsR[offs + n]);
                im -= math.dot(x_n, coeffsI[offs + n]);
            }

            output[i] = math.sqrt(rl * rl + im * im);
        }
    }

    #endregion
}
