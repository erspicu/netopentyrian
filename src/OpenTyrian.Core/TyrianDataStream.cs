namespace OpenTyrian.Core;

public sealed class TyrianDataStream : IDisposable
{
    private readonly Stream _stream;
    private readonly bool _leaveOpen;

    public TyrianDataStream(Stream stream, bool leaveOpen = false)
    {
        _stream = stream;
        _leaveOpen = leaveOpen;
    }

    public long Length => _stream.Length;

    public long Position
    {
        get => _stream.Position;
        set => _stream.Position = value;
    }

    public byte ReadByte()
    {
        int value = _stream.ReadByte();
        if (value < 0)
        {
            throw new EndOfStreamException("Unexpected end of stream while reading byte.");
        }

        return (byte)value;
    }

    public sbyte ReadSByte()
    {
        return unchecked((sbyte)ReadByte());
    }

    public bool ReadBoolean()
    {
        return ReadByte() != 0;
    }

    public ushort ReadUInt16()
    {
        byte[] buffer = ReadBytes(2);
        return (ushort)(buffer[0] | (buffer[1] << 8));
    }

    public short ReadInt16()
    {
        return unchecked((short)ReadUInt16());
    }

    public uint ReadUInt32()
    {
        byte[] buffer = ReadBytes(4);
        return (uint)(buffer[0] |
            (buffer[1] << 8) |
            (buffer[2] << 16) |
            (buffer[3] << 24));
    }

    public int ReadInt32()
    {
        return unchecked((int)ReadUInt32());
    }

    public byte[] ReadBytes(int count)
    {
        byte[] buffer = new byte[count];
        ReadExactly(buffer, 0, count);
        return buffer;
    }

    public void ReadExactly(byte[] buffer)
    {
        ReadExactly(buffer, 0, buffer.Length);
    }

    public void ReadExactly(byte[] buffer, int offset, int count)
    {
        int totalRead = 0;
        while (totalRead < count)
        {
            int read = _stream.Read(buffer, offset + totalRead, count - totalRead);
            if (read <= 0)
            {
                throw new EndOfStreamException("Unexpected end of stream while reading data.");
            }

            totalRead += read;
        }
    }

    public void Seek(long offset, SeekOrigin origin)
    {
        _stream.Seek(offset, origin);
    }

    public void Dispose()
    {
        if (!_leaveOpen)
        {
            _stream.Dispose();
        }
    }
}
