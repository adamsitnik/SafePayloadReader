using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;

namespace System.Runtime.Serialization.BinaryFormat;

public abstract class SerializationRecord
{
    internal const int NoId = 0;

    internal SerializationRecord() // others can't derive from this type
    {
    }

    public abstract RecordType RecordType { get; }

    internal virtual int ObjectId => NoId;

    public virtual bool IsSerializedInstanceOf(Type type) => false;

    internal virtual object? GetValue() => this;

    /// <summary>
    ///  Reads an object member value of <paramref name="type"/> with optional clarifying <paramref name="typeInfo"/>.
    /// </summary>
    /// <exception cref="SerializationException"><paramref name="type"/> was unexpected.</exception>
    internal static object ReadValue(
        BinaryReader reader,
        RecordMap recordMap,
        BinaryType type,
        object? typeInfo) => type switch
        {
            BinaryType.Primitive => ReadPrimitiveType(reader, (PrimitiveType)typeInfo!),
            BinaryType.String => SafePayloadReader.ReadNext(reader, recordMap, AllowedRecordTypes.BinaryObjectString | AllowedRecordTypes.ObjectNull, out _),
            BinaryType.Object => SafePayloadReader.ReadNext(reader, recordMap, AllowedRecordTypes.Classes, out _),
            BinaryType.StringArray => SafePayloadReader.ReadNext(reader, recordMap, AllowedRecordTypes.ArraySingleString | AllowedRecordTypes.MemberReference, out _),
            BinaryType.PrimitiveArray => SafePayloadReader.ReadNext(reader, recordMap, AllowedRecordTypes.ArraySinglePrimitive | AllowedRecordTypes.ObjectNull | AllowedRecordTypes.MemberReference, out _),
            BinaryType.Class or BinaryType.SystemClass => SafePayloadReader.ReadNext(reader, recordMap, AllowedRecordTypes.MemberReferences | AllowedRecordTypes.Nulls, out _),
            BinaryType.ObjectArray => SafePayloadReader.ReadNext(reader, recordMap, AllowedRecordTypes.ArraySingleObject | AllowedRecordTypes.MemberReference, out _),
            _ => throw new SerializationException("Invalid binary type."),
        };

    /// <summary>
    ///  Reads a primitive of <paramref name="primitiveType"/> from the given <paramref name="reader"/>.
    /// </summary>
    private protected static object ReadPrimitiveType(BinaryReader reader, PrimitiveType primitiveType)
        => primitiveType switch
    {
        PrimitiveType.Boolean => reader.ReadBoolean(),
        PrimitiveType.Byte => reader.ReadByte(),
        PrimitiveType.SByte => reader.ReadSByte(),
        PrimitiveType.Char => reader.ReadChar(),
        PrimitiveType.Int16 => reader.ReadInt16(),
        PrimitiveType.UInt16 => reader.ReadUInt16(),
        PrimitiveType.Int32 => reader.ReadInt32(),
        PrimitiveType.UInt32 => reader.ReadUInt32(),
        PrimitiveType.Int64 => reader.ReadInt64(),
        PrimitiveType.UInt64 => reader.ReadUInt64(),
        PrimitiveType.Single => reader.ReadSingle(),
        PrimitiveType.Double => reader.ReadDouble(),
        PrimitiveType.Decimal => decimal.Parse(reader.ReadString(), CultureInfo.InvariantCulture),
        PrimitiveType.DateTime => CreateDateTimeFromData(reader.ReadInt64()),
        PrimitiveType.TimeSpan => new TimeSpan(reader.ReadInt64()),
        // String is handled with a record, never on it's own
        _ => throw new SerializationException($"Failure trying to read primitive '{primitiveType}'"),
    };

    /// <summary>
    ///  Creates a <see cref="DateTime"/> object from raw data with validation.
    /// </summary>
    /// <exception cref="SerializationException"><paramref name="data"/> was invalid.</exception>
    internal static DateTime CreateDateTimeFromData(long data)
    {
        // Copied from System.Runtime.Serialization.Formatters.Binary.BinaryParser

        // Use DateTime's public constructor to validate the input, but we
        // can't return that result as it strips off the kind. To address
        // that, store the value directly into a DateTime via an unsafe cast.
        // See BinaryFormatterWriter.WriteDateTime for details.

        try
        {
            const long TicksMask = 0x3FFFFFFFFFFFFFFF;
            _ = new DateTime(data & TicksMask);
        }
        catch (ArgumentException ex)
        {
            // Bad data
            throw new SerializationException(ex.Message, ex);
        }

        return Unsafe.As<long, DateTime>(ref data);
    }

    private protected static SerializationRecord[] ReadRecords(BinaryReader reader, 
        RecordMap recordMap, int recordCount, AllowedRecordTypes allowed = AllowedRecordTypes.AnyData)
    {
        SerializationRecord[] records = new SerializationRecord[recordCount];
        for (int i = 0; i < records.Length;)
        {
            SerializationRecord record = SafePayloadReader.ReadNext(reader, recordMap, allowed, out _);

            Insert(records, ref i, record, ObjectNullRecord.Instance);
        }
        return records;
    }

    private protected static int Insert<T>(T?[] values, ref int index, object value, T? nullValue)
    {
        int nullCount = value switch
        {
            ObjectNullRecord => 1,
            ObjectNullMultiple256Record few => few.Count,
            ObjectNullMultipleRecord many => many.Count,
            _ => 0
        };

        if (nullCount > 0)
        {
            if (index + nullCount > values.Length)
            {
                throw new SerializationException($"Unexpected Null Record count: {nullCount}.");
            }

            do
            {
                values[index++] = nullValue;
                nullCount--;
            } while (nullCount > 0);
        }
        else
        {
            values[index++] = (T)value;
        }

        return nullCount;
    }
}