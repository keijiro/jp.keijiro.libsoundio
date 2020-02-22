using System.Collections.Generic;
using System.Linq;
using UnityEngine.LowLevel;

namespace UnitySioTest
{
    //
    // Singleton device driver class
    //
    public static class DeviceDriver
    {
        #region Public properties and methods

        public static int DeviceCount => Context.InputDeviceCount;
        public static int DefaultDeviceIndex => Context.DefaultInputDeviceIndex;

        public static string GetDeviceName(int index)
        {
            using(var dev = Context.GetInputDevice(index)) return dev.Name;
        }

        public static InputStream OpenInputStream(int deviceIndex)
        {
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

        #region SoundIO context

        static SoundIO.Context Context => GetContextWithLazyInitialization();

        static SoundIO.Context _context;

        static SoundIO.Context GetContextWithLazyInitialization()
        {
            if (_context == null)
            {
                _context = SoundIO.Context.Create();
                _context.Connect();
                _context.FlushEvents();

                InsertPlayerLoopSystem();

                #if UNITY_EDITOR
                // We use not only PlayerLoopSystem but also the
                // EditorApplication.update callback because the PlayerLoop events
                // are not invoked in the edit mode.
                UnityEditor.EditorApplication.update += () => Update();
                #endif
            }
            return _context;
        }

        #endregion

        #region Internal methods

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

            // Reconstruct the input stream list when an invalid one was found.
            if (foundInvalid)
                _inputStreams = _inputStreams.Where(s => s.IsValid).ToList();
        }

        #endregion

        #region PlayerLoopSystem implementation

        static void InsertPlayerLoopSystem()
        {
            var customSystem = new PlayerLoopSystem() {
                type = typeof(DeviceDriver),
                updateDelegate = () => DeviceDriver.Update()
            };

            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();

            for (var i = 0; i < playerLoop.subSystemList.Length; i++)
            {
                ref var phase = ref playerLoop.subSystemList[i];
                if (phase.type == typeof(UnityEngine.PlayerLoop.EarlyUpdate))
                {
                    phase.subSystemList =
                        phase.subSystemList.Concat(new [] { customSystem }).ToArray();
                    break;
                }
            }

            PlayerLoop.SetPlayerLoop(playerLoop);
        }

        #endregion
    }
}
