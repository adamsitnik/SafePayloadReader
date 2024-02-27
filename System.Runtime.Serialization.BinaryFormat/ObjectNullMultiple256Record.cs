using System.IO;

namespace System.Runtime.Serialization.BinaryFormat;

internal sealed class ObjectNullMultiple256Record : SerializationRecord
{
    internal byte Count { get; }

    internal ObjectNullMultiple256Record(byte count) => Count = count;

    public override RecordType RecordType => RecordType.ObjectNullMultiple256;

    internal static ObjectNullMultiple256Record Parse(BinaryReader reader)
        => new(reader.ReadByte());
}
