using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

// Extension methods for NativeSlice <-> ReadOnlySpan conversion

static class SpanNativeSliceExtensions
{
    static AtomicSafetyHandle SpanSafetyHandle
      = GetSpanSafetyHandle();

    static AtomicSafetyHandle _spanSafetyHandle;

    static AtomicSafetyHandle GetSpanSafetyHandle()
    {
        _spanSafetyHandle = AtomicSafetyHandle.Create();
        return _spanSafetyHandle;
    }

    public unsafe static NativeSlice<T>
      GetNativeSlice<T>(this ReadOnlySpan<T> span) where T : unmanaged
    {
        fixed (void* ptr = &span.GetPinnableReference())
        {
            var array = NativeSliceUnsafeUtility.
              ConvertExistingDataToNativeSlice<T>(ptr, sizeof(T), span.Length);
            #if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeSliceUnsafeUtility.
              SetAtomicSafetyHandle(ref array, SpanSafetyHandle);
            #endif
            return array;
        }
    }

    public unsafe static ReadOnlySpan<T>
      GetReadOnlySpan<T>(this NativeSlice<T> slice) where T : unmanaged
    {
        return new Span<T>(
          NativeSliceUnsafeUtility.GetUnsafeReadOnlyPtr(slice),
          slice.Length
        );
    }
}
