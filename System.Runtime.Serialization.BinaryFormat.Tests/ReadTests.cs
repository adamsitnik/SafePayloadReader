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

    /// <summary>
    /// Useful for very large inputs
    /// </summary>
    protected static FileStream SerializeToFile<T>(T instance) where T : notnull
    {
        string path = Path.GetTempFileName();
        FileStream fs = new(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, 
            FileShare.None, bufferSize: 100_000, FileOptions.DeleteOnClose);
#pragma warning disable SYSLIB0011 // Type or member is obsolete
        BinaryFormatter binaryFormatter = new();
#pragma warning restore SYSLIB0011 // Type or member is obsolete
        binaryFormatter.Serialize(fs, instance);
        fs.Flush();
        fs.Position = 0;
        return fs;
    }

    protected static void WriteSerializedStreamHeader(BinaryWriter writer, int major = 1, int minor = 0)
    {
        writer.Write((byte)RecordType.SerializedStreamHeader);
        writer.Write(1); // root ID
        writer.Write(1); // header ID
        writer.Write(major); // major version
        writer.Write(minor); // minor version
    }

    protected static void WriteBinaryLibrary(BinaryWriter writer, int objectId, string libraryName)
    {
        writer.Write((byte)RecordType.BinaryLibrary);
        writer.Write(objectId);
        writer.Write(libraryName);
    }
}