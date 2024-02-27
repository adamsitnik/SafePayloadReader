using System.IO;

namespace System.Runtime.Serialization.BinaryFormat;

/// <summary>
///  Multiple null object record.
/// </summary>
/// <remarks>
///  <para>
///   <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/f4abb5dd-aab7-4e0a-9d77-1d6c99f5779e">
///    [MS-NRBF] 2.5.5
///   </see>
///  </para>
/// </remarks>
internal sealed class ObjectNullMultipleRecord : SerializationRecord
{
    internal int Count { get; }

    private ObjectNullMultipleRecord(int count) => Count = count;

    public override RecordType RecordType => RecordType.ObjectNullMultiple;

    internal static ObjectNullMultipleRecord Parse(BinaryReader reader)
    {
        int count = reader.ReadInt32();
        if (count <= byte.MaxValue)
        {
            throw new SerializationException($"Unexpected count: {count}");
        }
        return new(count);
    }
}