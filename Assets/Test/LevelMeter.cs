using SoundIO.SimpleDriver;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

// Level meter with low/mid/high filter bank

public sealed class LevelMeter : MonoBehaviour
{
    #region Editable attributes

    [SerializeField] DeviceSelector _selector = null;
    [SerializeField] float _range = 60;
    [SerializeField] RectTransform _bypassMeter = null;
    [SerializeField] RectTransform _lowPassMeter = null;
    [SerializeField] RectTransform _bandPassMeter = null;
    [SerializeField] RectTransform _highPassMeter = null;

    #endregion

    #region Filter + RMS level calculation job

    [Unity.Burst.BurstCompile(CompileSynchronously = true)]
    struct FilterRmsJob : IJob
    {
        [ReadOnly] public NativeSlice<float> Input;
        [WriteOnly] public NativeArray<float4> Output;
        public NativeArray<MultibandFilter> Filter;

        public void Execute()
        {
            var filter = Filter[0];

            // Square sum
            var ss = float4.zero;

            for (var i = 0; i < Input.Length; i++)
            {
                var vf = filter.FeedSample(Input[i]);
                ss += vf * vf;
            }

            // Root mean square
            var rms = math.sqrt(ss / Input.Length);

            // RMS in dBFS
            // Full scale sin wave = 0 dBFS : refLevel = 1/sqrt(2)
            const float refLevel = 0.7071f;
            const float zeroOffset = 1.5849e-13f;
            var level = 20 * math.log10(rms / refLevel + zeroOffset);

            // Output
            Output[0] = level;
            Filter[0] = filter;
        }
    }

    #endregion

    #region MonoBehaviour implementation

    MultibandFilter _filter;

    void Update()
    {
        // Single element native array used to share structs with the job.
        var tempFilter = new NativeArray<MultibandFilter>
          (1, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

        var tempLevel = new NativeArray<float4>
          (1, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

        // Filter update
        _filter.SetParameter(960.0f / _selector.SampleRate, 0.15f);
        tempFilter[0] = _filter;

        // Run the job on the main thread.
        new FilterRmsJob { Input = _selector.AudioData.GetNativeSlice(),
                           Filter = tempFilter, Output = tempLevel }.Run();

        // Preserve the filter state.
        _filter = tempFilter[0];

        // Meter scale
        var sc = math.max(0, _range + tempLevel[0]) / _range;

        // Apply to rect-transforms.
          _bypassMeter.transform.localScale = new Vector3(sc.x, 1, 1);
         _lowPassMeter.transform.localScale = new Vector3(sc.y, 1, 1);
        _bandPassMeter.transform.localScale = new Vector3(sc.z, 1, 1);
        _highPassMeter.transform.localScale = new Vector3(sc.w, 1, 1);

        // Cleaning the temporaries up.
        tempFilter.Dispose();
        tempLevel.Dispose();
    }

    #endregion
}
