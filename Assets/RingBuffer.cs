using System;

public sealed class RingBuffer
{
    byte[] _buffer;

    ulong _readCount;
    ulong _writeCount;

    public RingBuffer(int capacity) => _buffer = new byte[capacity];

    public int Capacity => _buffer.Length;
    public int FillCount => (int)(_writeCount - _readCount);
    public int FreeCount => Capacity - FillCount;

    int  ReadOffset => (int)( _readCount % (ulong)Capacity);
    int WriteOffset => (int)(_writeCount % (ulong)Capacity);

    public void Clear()
    {
        _readCount = _writeCount = 0;
    }

    public void Write(ReadOnlySpan<byte> data)
    {
        if (FreeCount == 0)
        {
            UnityEngine.Debug.LogWarning("RingBuffer overflow");
            return;
        }

        if (data.Length > FreeCount)
        {
            UnityEngine.Debug.LogWarning("RingBuffer overflow");
            data = data.Slice(data.Length - FreeCount);
        }

        var rp = ReadOffset;
        var wp = WriteOffset;

        var head_rp = new Span<byte>(_buffer, 0, rp);
        var wp_tail = new Span<byte>(_buffer, wp, Capacity - wp);

        if (rp > wp || data.Length < wp_tail.Length)
        {
            data.CopyTo(wp_tail);
        }
        else
        {
            data.Slice(0, wp_tail.Length).CopyTo(wp_tail);
            data.Slice(wp_tail.Length).CopyTo(head_rp);
        }

        _writeCount += (ulong)data.Length;
    }

    public void WriteEmpty(int length)
    {
        UnityEngine.Debug.Assert(length <= FreeCount);

        var rp = ReadOffset;
        var wp = WriteOffset;

        var head_rp = new Span<byte>(_buffer, 0, rp);
        var wp_tail = new Span<byte>(_buffer, wp, Capacity - wp);

        if (rp > wp)
        {
            wp_tail.Slice(0, length).Fill(0);
        }
        else
        {
            wp_tail.Fill(0);
            head_rp.Slice(0, length - wp_tail.Length).Fill(0);
        }

        _writeCount += (ulong)length;
    }

    public void Read(Span<byte> dest)
    {
        UnityEngine.Debug.Assert(dest.Length <= FillCount);

        var rp = ReadOffset;
        var wp = WriteOffset;

        if (wp > rp || dest.Length < Capacity - rp)
        {
            new Span<byte>(_buffer, rp, dest.Length).CopyTo(dest);
        }
        else
        {
            var part = Capacity - rp;
            new Span<byte>(_buffer, rp, part).CopyTo(dest);
            new Span<byte>(_buffer, 0, dest.Length - part).CopyTo(dest.Slice(part));
        }

        _readCount += (ulong)dest.Length;
    }
}
