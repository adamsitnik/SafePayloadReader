
using System.Globalization;
using System.IO;

namespace System.Runtime.Serialization.BinaryFormat;

/// <summary>
///  Single dimensional array of a primitive type.
/// </summary>
/// <remarks>
///  <para>
///   <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/3a50a305-5f32-48a1-a42a-c34054db310b">
///    [MS-NRBF] 2.4.3.3
///   </see>
///  </para>
/// </remarks>
internal class ArraySinglePrimitiveRecord<T> : ArrayRecord<T>
    where T : unmanaged
{
    internal ArraySinglePrimitiveRecord(int id, T[] values) : base(values) => Id = id;

    public override RecordType RecordType => RecordType.ArraySinglePrimitive;

    internal override int Id { get; }

    public override bool IsSerializedInstanceOf(Type type) => typeof(T[]) == type;

    internal override object GetValue() => Values;

    internal static SerializationRecord Parse(BinaryReader reader)
    {
        ArrayInfo arrayInfo = ArrayInfo.Parse(reader);
        PrimitiveType primitiveType = (PrimitiveType)reader.ReadByte();

        int id = arrayInfo.ObjectId;
        int length = arrayInfo.Length;

        SerializationRecord record = primitiveType switch
        {
            PrimitiveType.Boolean => new ArraySinglePrimitiveRecord<bool>(id, ReadPrimitiveTypes<bool>(reader, length)),
            PrimitiveType.Byte => new ArraySinglePrimitiveRecord<byte>(id, ReadPrimitiveTypes<byte>(reader, length)),
            PrimitiveType.SByte => new ArraySinglePrimitiveRecord<sbyte>(id, ReadPrimitiveTypes<sbyte>(reader, length)),
            PrimitiveType.Char => new ArraySinglePrimitiveRecord<char>(id, ReadPrimitiveTypes<char>(reader, length)),
            PrimitiveType.Int16 => new ArraySinglePrimitiveRecord<short>(id, ReadPrimitiveTypes<short>(reader, length)),
            PrimitiveType.UInt16 => new ArraySinglePrimitiveRecord<ushort>(id, ReadPrimitiveTypes<ushort>(reader, length)),
            PrimitiveType.Int32 => new ArraySinglePrimitiveRecord<int>(id, ReadPrimitiveTypes<int>(reader, length)),
            PrimitiveType.UInt32 => new ArraySinglePrimitiveRecord<uint>(id, ReadPrimitiveTypes<uint>(reader, length)),
            PrimitiveType.Int64 => new ArraySinglePrimitiveRecord<long>(id, ReadPrimitiveTypes<long>(reader, length)),
            PrimitiveType.UInt64 => new ArraySinglePrimitiveRecord<ulong>(id, ReadPrimitiveTypes<ulong>(reader, length)),
            PrimitiveType.Single => new ArraySinglePrimitiveRecord<float>(id, ReadPrimitiveTypes<float>(reader, length)),
            PrimitiveType.Double => new ArraySinglePrimitiveRecord<double>(id, ReadPrimitiveTypes<double>(reader, length)),
            PrimitiveType.Decimal => new ArraySinglePrimitiveRecord<decimal>(id, ReadPrimitiveTypes<decimal>(reader, length)),
            PrimitiveType.DateTime => new ArraySinglePrimitiveRecord<DateTime>(id, ReadPrimitiveTypes<DateTime>(reader, length)),
            PrimitiveType.TimeSpan => new ArraySinglePrimitiveRecord<TimeSpan>(id, ReadPrimitiveTypes<TimeSpan>(reader, length)),
            _ => throw new SerializationException($"Failure trying to read primitive '{primitiveType}'"),
        };

        return record;
    }

    private static T[] ReadPrimitiveTypes<T>(BinaryReader reader, int count)
        where T : unmanaged
    {
        // Special casing byte for performance.
        if (typeof(T) == typeof(byte))
        {
            byte[] bytes = reader.ReadBytes(count);
            return (T[])(object)bytes;
        }

        T[] values = new T[count];
        for (int i = 0; i < values.Length; i++)
        {
            if (typeof(T) == typeof(bool))
            {
                values[i] = (T)(object)reader.ReadBoolean();
            }
            else if (typeof(T) == typeof(sbyte))
            {
                values[i] = (T)(object)reader.ReadSByte();
            }
            else if (typeof(T) == typeof(char))
            {
                values[i] = (T)(object)reader.ReadChar();
            }
            else if (typeof(T) == typeof(short))
            {
                values[i] = (T)(object)reader.ReadInt16();
            }
            else if (typeof(T) == typeof(ushort))
            {
                values[i] = (T)(object)reader.ReadUInt16();
            }
            else if (typeof(T) == typeof(int))
            {
                values[i] = (T)(object)reader.ReadInt32();
            }
            else if (typeof(T) == typeof(uint))
            {
                values[i] = (T)(object)reader.ReadUInt32();
            }
            else if (typeof(T) == typeof(long))
            {
                values[i] = (T)(object)reader.ReadInt64();
            }
            else if (typeof(T) == typeof(ulong))
            {
                values[i] = (T)(object)reader.ReadUInt64();
            }
            else if (typeof(T) == typeof(float))
            {
                values[i] = (T)(object)reader.ReadSingle();
            }
            else if (typeof(T) == typeof(double))
            {
                values[i] = (T)(object)reader.ReadDouble();
            }
            else if (typeof(T) == typeof(decimal))
            {
                values[i] = (T)(object)decimal.Parse(reader.ReadString(), CultureInfo.InvariantCulture);
            }
            else if (typeof(T) == typeof(DateTime))
            {
                values[i] = (T)(object)CreateDateTimeFromData(reader.ReadInt64());
            }
            else if (typeof(T) == typeof(TimeSpan))
            {
                values[i] = (T)(object)new TimeSpan(reader.ReadInt64());
            }
            else
            {
                throw new SerializationException($"Failure trying to read primitive '{typeof(T)}'");
            }
        }

        return values;
    }
}
