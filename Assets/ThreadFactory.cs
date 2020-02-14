using System.Collections.Generic;
using System.Threading;
using IntPtr = System.IntPtr;
using SIO = SoundIO.NativeMethods;
using GCHandle = System.Runtime.InteropServices.GCHandle;

static class ThreadFactory
{
    static HashSet<GCHandle> _handles = new HashSet<GCHandle>();

    static IntPtr CreateThread(IntPtr userData)
    {
        UnityEngine.Debug.Log("Create " + userData);
        var thread = new Thread(() => SIO.RunThread(userData));
        var handle = GCHandle.Alloc(thread);
        thread.Start();
        return GCHandle.ToIntPtr(handle);
    }

    static void DestroyThread(IntPtr pThread)
    {
        UnityEngine.Debug.Log("Destroy " + pThread);
        var handle = GCHandle.FromIntPtr(pThread);
        var thread = (Thread)handle.Target;
        thread.Join();
        _handles.Remove(handle);
        handle.Free();
    }

    static SIO.ThreadFactoryData _data = new SIO.ThreadFactoryData
    {
        CreateThread = CreateThread,
        DestroyThread = DestroyThread
    };

    public static void Initialize()
    {
        SIO.SetThreadFactory(_data);
    }
}
