using System;

public sealed class FifoBuffer
{
    byte[] _buffer;

    public FifoBuffer(int size) => _buffer = new byte[size];

    public ReadOnlySpan<byte> ReadSpan => new ReadOnlySpan<byte>(_buffer);

    public void Push(ReadOnlySpan<byte> data)
    {
        var buf = new Span<byte>(_buffer);

        if (data.Length < buf.Length)
        {
            buf.Slice(data.Length, buf.Length - data.Length).CopyTo(buf);
            data.CopyTo(buf.Slice(buf.Length - data.Length));
        }
        else
        {
            data.Slice(data.Length - buf.Length).CopyTo(buf);
        }
    }

    public void PushEmpty(int size)
    {
        var buf = new Span<byte>(_buffer);

        if (size < buf.Length)
        {
            buf.Slice(size, buf.Length - size).CopyTo(buf);
            buf.Slice(buf.Length - size).Fill(0);
        }
        else
        {
            buf.Fill(0);
        }
    }
}

