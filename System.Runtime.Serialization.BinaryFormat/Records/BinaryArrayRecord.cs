using System.Collections.Generic;
using System.IO;

namespace System.Runtime.Serialization.BinaryFormat;

internal sealed class BinaryArrayRecord<T> : ArrayRecord<T>
{
    private BinaryArrayRecord(ArrayInfo arrayInfo, MemberTypeInfo memberTypeInfo, int[] lengths, int[] offsets)
        : base(arrayInfo)
    {
        MemberTypeInfo = memberTypeInfo;
        Lengths = lengths;
        Offsets = offsets;
        Values = new();
    }

    public override RecordType RecordType => RecordType.BinaryArray;

    private MemberTypeInfo MemberTypeInfo { get; }

    private int[] Lengths { get; }

    private int[] Offsets { get; }

    internal List<object> Values { get; }

    public override bool IsSerializedInstanceOf(Type type)
    {
        if (!type.IsArray || type.GetArrayRank() == ArrayInfo.Rank)
        {
            return false;
        }

        // TODO: compare offsets (there seems to be no reflection API for that)
        // TODO: compare the type
        return true;
    }

    protected override T?[] Deserialize(bool allowNulls)
    {
        T?[] values = new T?[Length];

        for (int recordIndex = 0, valueIndex = 0; recordIndex < Values.Count; recordIndex++)
        {
            object item = Values[recordIndex];

            if (item is MemberReferenceRecord referenceRecord)
            {
                item = referenceRecord.GetReferencedRecord();
            }

            if (item is T value)
            {
                values[valueIndex++] = value;
                continue;
            }

            if (!allowNulls)
            {
                throw new SerializationException("The array contained null(s)");
            }

            int nullCount = ((NullsRecord)item).NullCount;
            do
            {
                values[valueIndex++] = default;
                nullCount--;
            } while (nullCount > 0);
        }

        return values;
    }

    internal override object GetValue() => Deserialize();

    internal static SerializationRecord Parse(BinaryReader reader)
    {
        int objectId = reader.ReadInt32();

        byte typeByte = reader.ReadByte();
        if (typeByte < 0 || typeByte > 5 )
        {
            throw new SerializationException($"Unknown binary array type: {typeByte}");
        }
        BinaryArrayType arrayType = (BinaryArrayType)typeByte;
        int rank = reader.ReadInt32();

        bool isRectangular = arrayType is BinaryArrayType.Rectangular or BinaryArrayType.RectangularOffset;

        if (rank < 1 || rank > 32 
            || (rank != 1 && !isRectangular)
            || (rank == 1 && isRectangular))
        {
            throw new SerializationException($"Invalid array rank ({rank}) for {arrayType}.");
        }

        int[] lengths = new int[rank]; // adversary-controlled, but acceptable since upper limit of 32
        for (int i = 0; i < lengths.Length; i++)
        {
            lengths[i] = ArrayInfo.ParseValidArrayLength(reader);
        }

        long totalElementCount = lengths[0];
        for (int i = 1; i < lengths.Length; i++)
        {
            totalElementCount *= lengths[i];

            if (totalElementCount > uint.MaxValue)
            {
                throw new SerializationException("Max array size exceeded"); // max array size exceeded
            }
        }

        int[] offsets = new int[rank]; // zero-init; adversary-controlled, but acceptable since upper limit of 32
        if (arrayType is BinaryArrayType.SingleOffset or BinaryArrayType.JaggedOffset or BinaryArrayType.RectangularOffset)
        {
            for (int i = 0; i < offsets.Length; i++)
            {
                offsets[i] = reader.ReadInt32();

                long maxIndex = lengths[i] + offsets[i];
                if (maxIndex > int.MaxValue)
                {
                    throw new SerializationException("Invalid length and offset");
                }
            }   
        }

        MemberTypeInfo memberTypeInfo = MemberTypeInfo.Parse(reader, 1);
        ArrayInfo arrayInfo = new(objectId, (uint)totalElementCount, arrayType, rank);

        if (arrayType is BinaryArrayType.Rectangular or BinaryArrayType.RectangularOffset)
        {
            return RectangularArrayRecord.Create(arrayInfo, memberTypeInfo, lengths, offsets);
        }

        switch (memberTypeInfo.Infos[0].BinaryType)
        {
            case BinaryType.Primitive:
                return MapPrimitive((PrimitiveType)memberTypeInfo.Infos[0].AdditionalInfo!, arrayInfo, memberTypeInfo, lengths, offsets);
            case BinaryType.String:
                return new BinaryArrayRecord<string>(arrayInfo, memberTypeInfo, lengths, offsets);
            case BinaryType.Object:
                return new BinaryArrayRecord<object>(arrayInfo, memberTypeInfo, lengths, offsets);
            case BinaryType.SystemClass:
            case BinaryType.Class:
                return new BinaryArrayRecord<ClassRecord>(arrayInfo, memberTypeInfo, lengths, offsets);
            case BinaryType.ObjectArray:
                return new BinaryArrayRecord<ArraySingleObjectRecord>(arrayInfo, memberTypeInfo, lengths, offsets);
            case BinaryType.StringArray:
                return new BinaryArrayRecord<ArraySingleStringRecord>(arrayInfo, memberTypeInfo, lengths, offsets);
            case BinaryType.PrimitiveArray:
                return MapPrimitiveArray((PrimitiveType)memberTypeInfo.Infos[0].AdditionalInfo!, arrayInfo, memberTypeInfo, lengths, offsets);
            default:
                throw ThrowHelper.InvalidBinaryType(memberTypeInfo.Infos[0].BinaryType);
        }
    }

    internal override void HandleNextValue(object value, NextInfo info)
        => HandleNext(value, info, size: 1);

    internal override void HandleNextRecord(SerializationRecord nextRecord, NextInfo info)
        => HandleNext(nextRecord, info, size: nextRecord is NullsRecord nullsRecord ? nullsRecord.NullCount : 1);

    private void HandleNext(object value, NextInfo info, int size)
    {
        ValuesToRead -= size;

        if (ValuesToRead < 0)
        {
            // The only way to get here is to read a multiple null item with Count
            // larger than the number of array items that were left to read.
            ThrowHelper.ThrowUnexpectedNullRecordCount();
        }
        else if (ValuesToRead > 0)
        {
            info.Stack.Push(info);
        }

        Values.Add(value);
    }

    internal override (AllowedRecordTypes allowed, PrimitiveType primitiveType) GetAllowedRecordType()
    {
        (AllowedRecordTypes allowed, PrimitiveType primitiveType) = MemberTypeInfo.GetNextAllowedRecordType(0);

        if (allowed != AllowedRecordTypes.None)
        {
            // It's an array, it can also contain multiple nulls
            return (allowed | AllowedRecordTypes.Nulls, primitiveType);
        }

        return (allowed, primitiveType);
    }

    private static SerializationRecord MapPrimitive(PrimitiveType primitiveType,
        ArrayInfo arrayInfo, MemberTypeInfo memberTypeInfo, int[] lengths, int[] offsets)
    {
        switch (primitiveType)
        {
            case PrimitiveType.Boolean:
                return new BinaryArrayRecord<bool>(arrayInfo, memberTypeInfo, lengths, offsets);
            case PrimitiveType.Byte:
                return new BinaryArrayRecord<byte>(arrayInfo, memberTypeInfo, lengths, offsets);
            case PrimitiveType.Char:
                return new BinaryArrayRecord<char>(arrayInfo, memberTypeInfo, lengths, offsets);
            case PrimitiveType.Decimal:
                return new BinaryArrayRecord<decimal>(arrayInfo, memberTypeInfo, lengths, offsets);
            case PrimitiveType.Double:
                return new BinaryArrayRecord<double>(arrayInfo, memberTypeInfo, lengths, offsets);
            case PrimitiveType.Int16:
                return new BinaryArrayRecord<short>(arrayInfo, memberTypeInfo, lengths, offsets);
            case PrimitiveType.Int32:
                return new BinaryArrayRecord<int>(arrayInfo, memberTypeInfo, lengths, offsets);
            case PrimitiveType.Int64:
                return new BinaryArrayRecord<long>(arrayInfo, memberTypeInfo, lengths, offsets);
            case PrimitiveType.SByte:
                return new BinaryArrayRecord<sbyte>(arrayInfo, memberTypeInfo, lengths, offsets);
            case PrimitiveType.Single:
                return new BinaryArrayRecord<float>(arrayInfo, memberTypeInfo, lengths, offsets);
            case PrimitiveType.TimeSpan:
                return new BinaryArrayRecord<TimeSpan>(arrayInfo, memberTypeInfo, lengths, offsets);
            case PrimitiveType.DateTime:
                return new BinaryArrayRecord<DateTime>(arrayInfo, memberTypeInfo, lengths, offsets);
            case PrimitiveType.UInt16:
                return new BinaryArrayRecord<ushort>(arrayInfo, memberTypeInfo, lengths, offsets);
            case PrimitiveType.UInt32:
                return new BinaryArrayRecord<uint>(arrayInfo, memberTypeInfo, lengths, offsets);
            case PrimitiveType.UInt64:
                return new BinaryArrayRecord<ulong>(arrayInfo, memberTypeInfo, lengths, offsets);
            default:
                throw ThrowHelper.InvalidPrimitiveType(primitiveType);
        }
    }

    private static SerializationRecord MapPrimitiveArray(PrimitiveType primitiveType,
        ArrayInfo arrayInfo, MemberTypeInfo memberTypeInfo, int[] lengths, int[] offsets)
    {
        switch (primitiveType)
        {
            case PrimitiveType.Boolean:
                return new BinaryArrayRecord<ArraySinglePrimitiveRecord<bool>>(arrayInfo, memberTypeInfo, lengths, offsets);
            case PrimitiveType.Byte:
                return new BinaryArrayRecord<ArraySinglePrimitiveRecord<byte>>(arrayInfo, memberTypeInfo, lengths, offsets);
            case PrimitiveType.Char:
                return new BinaryArrayRecord<ArraySinglePrimitiveRecord<char>>(arrayInfo, memberTypeInfo, lengths, offsets);
            case PrimitiveType.Decimal:
                return new BinaryArrayRecord<ArraySinglePrimitiveRecord<decimal>>(arrayInfo, memberTypeInfo, lengths, offsets);
            case PrimitiveType.Double:
                return new BinaryArrayRecord<ArraySinglePrimitiveRecord<double>>(arrayInfo, memberTypeInfo, lengths, offsets);
            case PrimitiveType.Int16:
                return new BinaryArrayRecord<ArraySinglePrimitiveRecord<short>>(arrayInfo, memberTypeInfo, lengths, offsets);
            case PrimitiveType.Int32:
                return new BinaryArrayRecord<ArraySinglePrimitiveRecord<int>>(arrayInfo, memberTypeInfo, lengths, offsets);
            case PrimitiveType.Int64:
                return new BinaryArrayRecord<ArraySinglePrimitiveRecord<long>>(arrayInfo, memberTypeInfo, lengths, offsets);
            case PrimitiveType.SByte:
                return new BinaryArrayRecord<ArraySinglePrimitiveRecord<sbyte>>(arrayInfo, memberTypeInfo, lengths, offsets);
            case PrimitiveType.Single:
                return new BinaryArrayRecord<ArraySinglePrimitiveRecord<float>>(arrayInfo, memberTypeInfo, lengths, offsets);
            case PrimitiveType.TimeSpan:
                return new BinaryArrayRecord<ArraySinglePrimitiveRecord<TimeSpan>>(arrayInfo, memberTypeInfo, lengths, offsets);
            case PrimitiveType.DateTime:
                return new BinaryArrayRecord<ArraySinglePrimitiveRecord<DateTime>>(arrayInfo, memberTypeInfo, lengths, offsets);
            case PrimitiveType.UInt16:
                return new BinaryArrayRecord<ArraySinglePrimitiveRecord<ushort>>(arrayInfo, memberTypeInfo, lengths, offsets);
            case PrimitiveType.UInt32:
                return new BinaryArrayRecord<ArraySinglePrimitiveRecord<uint>>(arrayInfo, memberTypeInfo, lengths, offsets);
            case PrimitiveType.UInt64:
                return new BinaryArrayRecord<ArraySinglePrimitiveRecord<ulong>>(arrayInfo, memberTypeInfo, lengths, offsets);
            default:
                throw ThrowHelper.InvalidPrimitiveType(primitiveType);
        }
    }
}