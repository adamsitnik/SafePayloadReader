using System.IO;

namespace System.Runtime.Serialization.BinaryFormat;

internal sealed class MemberPrimitiveTypedRecord : SerializationRecord
{
    private MemberPrimitiveTypedRecord(PrimitiveType primitiveType, object value)
    {
        PrimitiveType = primitiveType;
        Value = value;
    }

    public override RecordType RecordType => RecordType.MemberPrimitiveTyped;

    internal PrimitiveType PrimitiveType { get; }

    internal object Value { get; }

    public override object? GetValue() => Value;

    public override bool IsSerializedInstanceOf(Type type) => type == Value.GetType();

    internal static MemberPrimitiveTypedRecord Parse(BinaryReader reader)
    {
        PrimitiveType primitiveType = (PrimitiveType)reader.ReadByte();
        object value = ReadPrimitiveType(reader, primitiveType);

        return new(primitiveType, value);
    }
}
