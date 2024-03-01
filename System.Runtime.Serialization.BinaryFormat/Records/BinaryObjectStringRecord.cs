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
    private BinaryObjectStringRecord(int objectId, string value)
    {
        ObjectId = objectId;
        Value = value;
    }

    public override RecordType RecordType => RecordType.BinaryObjectString;

    internal override int ObjectId { get; }

    internal string Value { get; }

    public override bool IsSerializedInstanceOf(Type type) => type == typeof(string);

    internal override object GetValue() => Value;

    internal static BinaryObjectStringRecord Parse(BinaryReader reader)
        => new(reader.ReadInt32(), reader.ReadString());
}