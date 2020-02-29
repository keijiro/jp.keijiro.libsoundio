// Simple driver for libsoundio
// https://github.com/keijiro/jp.keijiro.libsoundio

using System.Collections.Generic;
using System.Linq;
using UnityEngine.LowLevel;

namespace SoundIO.SimpleDriver
{
    //
    // Singleton-like device driver class
    //
    // This class manages a soundio context and input stream objects created in
    // the context. It also implements a Player Loop System and invokes the
    // Update methods in the EarlyUpdate phase every frame.
    //
    // Caveat: Doesn't support device addition/removal during run.
    //
    public static class DeviceDriver
    {
        #region Public properties and methods

        public static int DeviceCount => Context.InputDeviceCount;
        public static int DefaultDeviceIndex => Context.DefaultInputDeviceIndex;

        public static string GetDeviceName(int index)
        {
            using (var dev = Context.GetInputDevice(index))
                return dev.IsRaw ? "Raw: " + dev.Name : dev.Name;
        }

        public static InputStream OpenInputStream(int deviceIndex)
        {
            // Note: The ownership of the device object will be transferred to
            // the input stream object.
            var stream = new InputStream(Context.GetInputDevice(deviceIndex));

            if (stream.IsValid)
            {
                _inputStreams.Add(stream);
                return stream;
            }
            else
            {
                stream.Dispose();
                return null;
            }
        }

        #endregion

        #region SoundIO context management

        static Context Context => GetContextWithLazyInitialization();

        static Context _context;

        static Context GetContextWithLazyInitialization()
        {
            if (_context == null)
            {
                _context = Context.Create();

                _context.Connect();
                _context.FlushEvents();

                // Install the Player Loop System.
                InsertPlayerLoopSystem();
            }

            return _context;
        }

        #endregion

        #region Update method implementation

        static List<InputStream> _inputStreams = new List<InputStream>();

        static void Update()
        {
            Context.FlushEvents();

            // Update and validate the input streams.
            var foundInvalid = false;

            foreach (var stream in _inputStreams)
                if (stream.IsValid)
                    stream.Update();
                else
                    foundInvalid = true;

            // Reconstruct the input stream list when invalid ones were found.
            if (foundInvalid)
                _inputStreams = _inputStreams.Where(s => s.IsValid).ToList();
        }

        #endregion

        #region PlayerLoopSystem implementation

        static void InsertPlayerLoopSystem()
        {
            // Append a custom system to the Early Update phase.

            var customSystem = new PlayerLoopSystem()
            {
                type = typeof(DeviceDriver),
                updateDelegate = () => DeviceDriver.Update()
            };

            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();

            for (var i = 0; i < playerLoop.subSystemList.Length; i++)
            {
                ref var phase = ref playerLoop.subSystemList[i];
                if (phase.type == typeof(UnityEngine.PlayerLoop.EarlyUpdate))
                {
                    phase.subSystemList = phase.subSystemList.
                        Concat(new[]{ customSystem }).ToArray();
                    break;
                }
            }

            PlayerLoop.SetPlayerLoop(playerLoop);
        }

        #endregion
    }
}
