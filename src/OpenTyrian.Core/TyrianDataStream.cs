using System.Buffers.Binary;

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
        Span<byte> buffer = stackalloc byte[sizeof(ushort)];
        ReadExactly(buffer);
        return BinaryPrimitives.ReadUInt16LittleEndian(buffer);
    }

    public short ReadInt16()
    {
        Span<byte> buffer = stackalloc byte[sizeof(short)];
        ReadExactly(buffer);
        return BinaryPrimitives.ReadInt16LittleEndian(buffer);
    }

    public uint ReadUInt32()
    {
        Span<byte> buffer = stackalloc byte[sizeof(uint)];
        ReadExactly(buffer);
        return BinaryPrimitives.ReadUInt32LittleEndian(buffer);
    }

    public int ReadInt32()
    {
        Span<byte> buffer = stackalloc byte[sizeof(int)];
        ReadExactly(buffer);
        return BinaryPrimitives.ReadInt32LittleEndian(buffer);
    }

    public byte[] ReadBytes(int count)
    {
        byte[] buffer = new byte[count];
        ReadExactly(buffer);
        return buffer;
    }

    public void ReadExactly(Span<byte> buffer)
    {
        int totalRead = 0;
        while (totalRead < buffer.Length)
        {
            int read = _stream.Read(buffer[totalRead..]);
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
