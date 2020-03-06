using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

// Extension methods for NativeArray <-> ReadOnlySpan conversion

static class SpanNativeArrayExtensions
{
    static AtomicSafetyHandle SpanSafetyHandle
      = GetSpanSafetyHandle();

    static AtomicSafetyHandle _spanSafetyHandle;

    static AtomicSafetyHandle GetSpanSafetyHandle()
    {
        _spanSafetyHandle = AtomicSafetyHandle.Create();
        return _spanSafetyHandle;
    }

    public unsafe static NativeArray<T>
      GetNativeArray<T>(this ReadOnlySpan<T> span) where T : unmanaged
    {
        fixed (void* ptr = &span.GetPinnableReference())
        {
            var array = NativeArrayUnsafeUtility.
              ConvertExistingDataToNativeArray<T>
              (ptr, span.Length, Allocator.None);
            #if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.
              SetAtomicSafetyHandle(ref array, SpanSafetyHandle);
            #endif
            return array;
        }
    }

    public unsafe static ReadOnlySpan<T>
      GetReadOnlySpan<T>(this NativeArray<T> array) where T : unmanaged
    {
        return new Span<T>(
          NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(array),
          array.Length
        );
    }
}
