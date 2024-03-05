using System.IO;
using System.Text;
using Xunit;

namespace System.Runtime.Serialization.BinaryFormat.Tests;

public class EdgeCaseTests
{
    [Fact]
    public void ThrowsOnInvalidUtf8Input()
    {
        using MemoryStream stream = new();
        BinaryWriter writer = new(stream, Encoding.UTF8);

        WriteSerializedStreamHeader(writer, major: 1, minor: 0);

        byte[] invalidUtf8 = [(byte)'a', (byte)'b', 0xC0, (byte)'x', (byte)'y'];

        writer.Write((byte)RecordType.BinaryObjectString);
        writer.Write((int)1); // object ID
        writer.Write7BitEncodedInt(invalidUtf8.Length);
        writer.Write(invalidUtf8);
        writer.Write((byte)RecordType.MessageEnd);

        stream.Position = 0;
        Assert.Throws<DecoderFallbackException>(() => SafePayloadReader.Read(stream));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    public void ThrowsOnInvalidHeaderVersion(int major, int minor)
    {
        using MemoryStream stream = new();
        BinaryWriter writer = new(stream, Encoding.UTF8);

        WriteSerializedStreamHeader(writer, major, minor);

        stream.Position = 0;
        Assert.Throws<SerializationException>(() => SafePayloadReader.Read(stream));
    }

    private static void WriteSerializedStreamHeader(BinaryWriter writer, int major, int minor)
    {
        writer.Write((byte)RecordType.SerializedStreamHeader);
        writer.Write(1); // root ID
        writer.Write(1); // header ID
        writer.Write(major); // major version
        writer.Write(minor); // minor version
    }
}
