using System.IO;

namespace System.Runtime.Serialization.BinaryFormat;

/// <summary>
///  String record.
/// </summary>
/// <remarks>
///  <para>
///   <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/eb503ca5-e1f6-4271-a7ee-c4ca38d07996">
///    [MS-NRBF] 2.5.7
///   </see>
///  </para>
/// </remarks>
internal sealed class BinaryObjectStringRecord : SerializationRecord
{
    private readonly int _id;
    public string Value { get; }

    private BinaryObjectStringRecord(int objectId, string value)
    {
        _id = objectId;
        Value = value;
    }

    public override RecordType RecordType => RecordType.BinaryObjectString;

    internal override int Id => _id;

    internal static BinaryObjectStringRecord Parse(BinaryReader reader)
        => new(reader.ReadInt32(), reader.ReadString());
}