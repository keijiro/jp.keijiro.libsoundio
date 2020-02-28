//
// LTC (Linear timecode) data structure and decoder
//

using System;

namespace Ltc
{
    // Unpacked representation of LTC
    public struct Timecode
    {
        public int frame;
        public int second;
        public int minute;
        public int hour;
        public bool dropFrame;

        // Unpack LTC data into a timecode instance
        static public Timecode Unpack(ulong data)
        {
            var s1 = (int)((data      ) & 0xffff);
            var s2 = (int)((data >> 16) & 0xffff);
            var s3 = (int)((data >> 32) & 0xffff);
            var s4 = (int)((data >> 48) & 0xffff);

            return new Timecode
            {
                frame  = (s1 & 0xf) + ((s1 >> 8) & 3) * 10,
                second = (s2 & 0xf) + ((s2 >> 8) & 7) * 10,
                minute = (s3 & 0xf) + ((s3 >> 8) & 7) * 10,
                hour   = (s4 & 0xf) + ((s4 >> 8) & 3) * 10,
                dropFrame = (s1 & 0x400) != 0
            };
        }

        public override string ToString()
        {
            return $"{hour:D2}:{minute:D2}:{second:D2}:{frame:D2}";
        }
    }

    // Timecode decoder class that analyzes audio signals to extract LTC data
    public sealed class TimecodeDecoder
    {
        #region Public methods

        public Timecode LastTimecode { get; private set; }

        public void ParseAudioData(ReadOnlySpan<float> data)
        {
            foreach (var v in data) ProcessSample(v > 0.0f);
        }

        #endregion

        #region Internal state

        // 128 bit FIFO queue for the bit stream
        (ulong lo, ulong hi) _fifo;

        // Sample count from the last transition
        int _count;

        // Bit period (x100 fixed point value)
        int _period;

        // Transition counter for true bits
        bool _tick;

        // Current state (the previous sample value)
        bool _state;

        #endregion

        #region Private methods

        void ProcessSample(bool sample)
        {
            // Biphase mark code decoder with an adaptive bit-period estimator.

            // No transition?
            if (_state == sample)
            {
                // Just increment the counter.
                if (_count < 10000) _count++;
                return;
            }

            // Half period?
            if (_count < _period / 100)
            {
                // Second transition?
                if (_tick)
                {
                    ProcessBit(true); // Output: "1"
                    _tick = false;
                }
                else
                    _tick = true;
            }
            else
            {
                ProcessBit(false); // Output: "0"
                _tick = false;
            }

            // Adaptive estimation of the bit period
            _period = (_period * 99 + _count * 100) / 100;

            _state = sample;
            _count = 0;
        }

        void ProcessBit(bool bit)
        {
            const ushort sync = 0xbffc; // LTC sync word
            const ulong msb64 = 1ul << 63;

            // 64:64 combined FIFO
            var hi_lsb = (_fifo.hi & 1ul) != 0;
            _fifo.lo = (_fifo.lo >> 1) | (hi_lsb ? msb64 : 0ul);
            _fifo.hi = (_fifo.hi >> 1) | (   bit ? msb64 : 0ul);

            // LTC sync word detection
            if ((ushort)_fifo.hi == sync)
                LastTimecode = Timecode.Unpack(_fifo.lo);
        }

        #endregion
    }
}
