using System.IO;

namespace System.Runtime.Serialization.BinaryFormat;

public sealed class SerializedStreamHeaderRecord : SerializationRecord
{
    internal SerializedStreamHeaderRecord(int rootId, int headerId, int majorVersion, int minorVersion)
    {
        RootId = rootId;
        HeaderId = headerId;
        MajorVersion = majorVersion;
        MinorVersion = minorVersion;
    }

    /// <summary>
    ///  The id of the root object record.
    /// </summary>
    public int RootId { get; }

    /// <summary>
    ///  Ignored. BinaryFormatter puts out -1.
    /// </summary>
    public int HeaderId { get; }

    /// <summary>
    ///  Must be 1.
    /// </summary>
    public int MajorVersion { get; }

    /// <summary>
    ///  Must be 0.
    /// </summary>
    public int MinorVersion { get; }

    public override RecordType RecordType => RecordType.SerializedStreamHeader;

    internal static SerializedStreamHeaderRecord Parse(BinaryReader reader)
    {
        int rootId = reader.ReadInt32();
        int headerId = reader.ReadInt32();
        int majorVersion = reader.ReadInt32();
        int minorVersion = reader.ReadInt32();

        if (majorVersion != 1 || minorVersion != 0)
        {
            throw new SerializationException();
        }

        return new(rootId, headerId, majorVersion, minorVersion);
    }
}
