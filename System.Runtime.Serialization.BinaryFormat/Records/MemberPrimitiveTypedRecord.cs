using System.IO;

namespace System.Runtime.Serialization.BinaryFormat;

/// <summary>
///  Primitive value other than <see langword="string"/>.
/// </summary>
/// <remarks>
///  <para>
///   <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/c0a190b2-762c-46b9-89f2-c7dabecfc084">
///    [MS-NRBF] 2.5.1
///   </see>
///  </para>
/// </remarks>
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

    internal override object? GetValue() => Value;

    public override bool IsSerializedInstanceOf(Type type)
    {
        if (type is null) throw new ArgumentNullException(nameof(type));

        Type? expectedType = PrimitiveType switch
        {
            PrimitiveType.Boolean => typeof(bool),
            PrimitiveType.Byte => typeof(byte),
            PrimitiveType.Char => typeof(char),
            PrimitiveType.Decimal => typeof(decimal),
            PrimitiveType.Double => typeof(double),
            PrimitiveType.Int16 => typeof(short),
            PrimitiveType.Int32 => typeof(int),
            PrimitiveType.Int64 => typeof(long),
            PrimitiveType.SByte => typeof(sbyte),
            PrimitiveType.Single => typeof(float),
            PrimitiveType.TimeSpan => typeof(TimeSpan),
            PrimitiveType.DateTime => typeof(DateTime),
            PrimitiveType.UInt16 => typeof(ushort),
            PrimitiveType.UInt32 => typeof(uint),
            PrimitiveType.UInt64 => typeof(ulong),
            PrimitiveType.String => typeof(string),
            _ => null
        };

        return expectedType == type;
    }

    internal static MemberPrimitiveTypedRecord Parse(BinaryReader reader)
    {
        PrimitiveType primitiveType = (PrimitiveType)reader.ReadByte();
        object value = reader.ReadPrimitiveType(primitiveType);

        return new(primitiveType, value);
    }
}
