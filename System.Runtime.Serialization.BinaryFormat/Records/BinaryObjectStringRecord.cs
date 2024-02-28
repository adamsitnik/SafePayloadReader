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
    internal static readonly BinaryObjectStringRecord NullString = new(-1, null);

    private BinaryObjectStringRecord(int objectId, string? value)
    {
        Id = objectId;
        Value = value;
    }

    public override RecordType RecordType => RecordType.BinaryObjectString;

    internal override int Id { get; }

    internal string? Value { get; }

    public override bool IsSerializedInstanceOf(Type type) => type == typeof(string);

    public override object? GetValue() => Value;

    internal static BinaryObjectStringRecord Parse(BinaryReader reader)
        => new(reader.ReadInt32(), reader.ReadString());
}