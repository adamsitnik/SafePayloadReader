using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace System.Runtime.Serialization.BinaryFormat.Tests;

public abstract class ReadTests
{
    protected static MemoryStream Serialize<T>(T instance) where T : notnull
    {
        MemoryStream ms = new();
#pragma warning disable SYSLIB0011 // Type or member is obsolete
        BinaryFormatter binaryFormatter = new();
#pragma warning restore SYSLIB0011 // Type or member is obsolete
        binaryFormatter.Serialize(ms, instance);

        ms.Position = 0;
        return ms;
    }

    protected static void WriteSerializedStreamHeader(BinaryWriter writer, int major = 1, int minor = 0)
    {
        writer.Write((byte)RecordType.SerializedStreamHeader);
        writer.Write(1); // root ID
        writer.Write(1); // header ID
        writer.Write(major); // major version
        writer.Write(minor); // minor version
    }
}