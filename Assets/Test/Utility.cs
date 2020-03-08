using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

// Extension methods for NativeArray/NativeSlice <-> ReadOnlySpan conversion

static class SpanNativeSliceExtensions
{
    public unsafe static NativeSlice<T>
      GetNativeSlice<T>(this ReadOnlySpan<T> span, int offset, int stride)
      where T : unmanaged
    {
        fixed (void* ptr = &span.GetPinnableReference())
        {
            var headPtr = (T*)ptr + offset;
            var strideInByte = sizeof(T) * stride;
            var elementCount = span.Length / stride - offset / stride;

            var slice =
              NativeSliceUnsafeUtility.ConvertExistingDataToNativeSlice<T>
              (headPtr, strideInByte, elementCount);

          #if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeSliceUnsafeUtility.SetAtomicSafetyHandle
              (ref slice, AtomicSafetyHandle.GetTempUnsafePtrSliceHandle());
          #endif

            return slice;
        }
    }

    public unsafe static NativeSlice<T>
      GetNativeSlice<T>(this ReadOnlySpan<T> span)
      where T : unmanaged
      => GetNativeSlice(span, 0, 1);

    public unsafe static ReadOnlySpan<T>
      GetReadOnlySpan<T>(this NativeArray<T> array)
      where T : unmanaged
    {
        var ptr = NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(array);
        return new Span<T>(ptr, array.Length);
    }

    public unsafe static ReadOnlySpan<T>
      GetReadOnlySpan<T>(this NativeSlice<T> slice)
      where T : unmanaged
    {
        var ptr = NativeSliceUnsafeUtility.GetUnsafeReadOnlyPtr(slice);
        return new Span<T>(ptr, slice.Length);
    }
}
