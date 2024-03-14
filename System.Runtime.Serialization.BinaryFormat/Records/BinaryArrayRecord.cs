using System.Collections.Generic;
using System.IO;

namespace System.Runtime.Serialization.BinaryFormat;

internal sealed class BinaryArrayRecord<T> : ArrayRecord<T>
{
    private BinaryArrayRecord(ArrayInfo arrayInfo, MemberTypeInfo memberTypeInfo)
        : base(arrayInfo)
    {
        MemberTypeInfo = memberTypeInfo;
        Values = new();
    }

    public override RecordType RecordType => RecordType.BinaryArray;

    private MemberTypeInfo MemberTypeInfo { get; }

    internal List<object> Values { get; }

    protected override T?[] ToArrayOfT(bool allowNulls)
    {
        T?[] values = new T?[Length];

        for (int recordIndex = 0, valueIndex = 0; recordIndex < Values.Count; recordIndex++)
        {
            object item = Values[recordIndex];

            if (item is MemberReferenceRecord referenceRecord)
            {
                item = referenceRecord.GetReferencedRecord();
            }

            // // IntPtr[] and UIntPtr[] are not represented as arrays of primitives, but as arrays of System Classes
            if ((typeof(T) == typeof(IntPtr) || typeof(T) == typeof(UIntPtr))
                && item is SystemClassWithMembersAndTypesRecord systemRecord)
            {
                item = systemRecord.GetValue<T>()!;
            }

            if (item is T value)
            {
                values[valueIndex++] = value;
                continue;
            }

            if (!allowNulls)
            {
                ThrowHelper.ThrowArrayContainedNull();
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

    internal static ArrayRecord Parse(BinaryReader reader)
    {
        int objectId = reader.ReadInt32();

        byte typeByte = reader.ReadByte();
        if (typeByte < 0 || typeByte > 5 )
        {
            throw new SerializationException($"Unknown binary array type: {typeByte}");
        }
        ArrayType arrayType = (ArrayType)typeByte;
        int rank = reader.ReadInt32();

        bool isRectangular = arrayType is ArrayType.Rectangular or ArrayType.RectangularOffset;

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
        bool hasCustomOffset = false;
        if (arrayType is ArrayType.SingleOffset or ArrayType.JaggedOffset or ArrayType.RectangularOffset)
        {
            for (int i = 0; i < offsets.Length; i++)
            {
                int offset = reader.ReadInt32();

                if (offset < 0)
                {
                    throw new SerializationException("Invalid offset");
                }
                else if (offset > 0)
                {
                    hasCustomOffset = true;

                    long maxIndex = lengths[i] + offset;
                    if (maxIndex > int.MaxValue)
                    {
                        throw new SerializationException("Invalid length and offset");
                    }
                }

                offsets[i] = offset;
            }   
        }

        MemberTypeInfo memberTypeInfo = MemberTypeInfo.Parse(reader, 1);
        ArrayInfo arrayInfo = new(objectId, (uint)totalElementCount, arrayType, rank);

        if (isRectangular || hasCustomOffset)
        {
            return RectangularOrCustomOffsetArrayRecord.Create(arrayInfo, memberTypeInfo, lengths, offsets);
        }

        bool isJagged = arrayType is ArrayType.Jagged or ArrayType.JaggedOffset;

        return memberTypeInfo.Infos[0].BinaryType switch
        {
            BinaryType.Primitive => MapPrimitive(arrayInfo, memberTypeInfo),
            BinaryType.String => new BinaryArrayRecord<string>(arrayInfo, memberTypeInfo),
            BinaryType.Object => new BinaryArrayRecord<object>(arrayInfo, memberTypeInfo),
            // IntPtr[] and UIntPtr[] are not represented as arrays of primitives, but as arrays of System Classes
            BinaryType.SystemClass when !isJagged && memberTypeInfo.IsElementType(typeof(IntPtr))
                => new BinaryArrayRecord<IntPtr>(arrayInfo, memberTypeInfo),
            BinaryType.SystemClass when !isJagged && memberTypeInfo.IsElementType(typeof(UIntPtr))
                => new BinaryArrayRecord<UIntPtr>(arrayInfo, memberTypeInfo),
            BinaryType.SystemClass or BinaryType.Class when isJagged 
                => new JaggedArrayRecord<ClassRecord>(arrayInfo, memberTypeInfo),
            BinaryType.SystemClass or BinaryType.Class when !isJagged
                => new BinaryArrayRecord<ClassRecord>(arrayInfo, memberTypeInfo),
            BinaryType.ObjectArray => new JaggedArrayRecord<object>(arrayInfo, memberTypeInfo),
            BinaryType.StringArray => new JaggedArrayRecord<string> (arrayInfo, memberTypeInfo),
            BinaryType.PrimitiveArray => MapPrimitiveArray(arrayInfo, memberTypeInfo),
            _ => throw ThrowHelper.InvalidBinaryType(memberTypeInfo.Infos[0].BinaryType),
        };
    }

    private protected override void AddValue(object value) => Values.Add(value);

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

    private protected override bool IsElementType(Type typeElement)
        => MemberTypeInfo.IsElementType(typeElement);

    private static ArrayRecord MapPrimitive(ArrayInfo arrayInfo, MemberTypeInfo memberTypeInfo)
        => (PrimitiveType)memberTypeInfo.Infos[0].AdditionalInfo! switch
        {
            PrimitiveType.Boolean => new BinaryArrayRecord<bool>(arrayInfo, memberTypeInfo),
            PrimitiveType.Byte => new BinaryArrayRecord<byte>(arrayInfo, memberTypeInfo),
            PrimitiveType.Char => new BinaryArrayRecord<char>(arrayInfo, memberTypeInfo),
            PrimitiveType.Decimal => new BinaryArrayRecord<decimal>(arrayInfo, memberTypeInfo),
            PrimitiveType.Double => new BinaryArrayRecord<double>(arrayInfo, memberTypeInfo),
            PrimitiveType.Int16 => new BinaryArrayRecord<short>(arrayInfo, memberTypeInfo),
            PrimitiveType.Int32 => new BinaryArrayRecord<int>(arrayInfo, memberTypeInfo),
            PrimitiveType.Int64 => new BinaryArrayRecord<long>(arrayInfo, memberTypeInfo),
            PrimitiveType.SByte => new BinaryArrayRecord<sbyte>(arrayInfo, memberTypeInfo),
            PrimitiveType.Single => new BinaryArrayRecord<float>(arrayInfo, memberTypeInfo  ),
            PrimitiveType.TimeSpan => new BinaryArrayRecord<TimeSpan>(arrayInfo, memberTypeInfo),
            PrimitiveType.DateTime => new BinaryArrayRecord<DateTime>(arrayInfo, memberTypeInfo),
            PrimitiveType.UInt16 => new BinaryArrayRecord<ushort>(arrayInfo, memberTypeInfo),
            PrimitiveType.UInt32 => new BinaryArrayRecord<uint>(arrayInfo, memberTypeInfo),
            PrimitiveType.UInt64 => new BinaryArrayRecord<ulong>(arrayInfo, memberTypeInfo),
            _ => throw ThrowHelper.InvalidPrimitiveType((PrimitiveType)memberTypeInfo.Infos[0].AdditionalInfo!),
        };

    private static ArrayRecord MapPrimitiveArray(ArrayInfo arrayInfo, MemberTypeInfo typeInfo) 
        => (PrimitiveType)typeInfo.Infos[0].AdditionalInfo! switch
        {
            PrimitiveType.Boolean => new JaggedArrayRecord<bool>(arrayInfo, typeInfo),
            PrimitiveType.Byte => new JaggedArrayRecord<byte>(arrayInfo, typeInfo),
            PrimitiveType.Char => new JaggedArrayRecord<char>(arrayInfo, typeInfo),
            PrimitiveType.Decimal => new JaggedArrayRecord<decimal>(arrayInfo, typeInfo),
            PrimitiveType.Double => new JaggedArrayRecord<double>(arrayInfo, typeInfo),
            PrimitiveType.Int16 => new JaggedArrayRecord<short>(arrayInfo, typeInfo),
            PrimitiveType.Int32 => new JaggedArrayRecord<int>(arrayInfo, typeInfo),
            PrimitiveType.Int64 => new JaggedArrayRecord<long>(arrayInfo, typeInfo),
            PrimitiveType.SByte => new JaggedArrayRecord<sbyte>(arrayInfo, typeInfo),
            PrimitiveType.Single => new JaggedArrayRecord<float>(arrayInfo, typeInfo),
            PrimitiveType.TimeSpan => new JaggedArrayRecord<TimeSpan>(arrayInfo, typeInfo),
            PrimitiveType.DateTime => new JaggedArrayRecord<DateTime>(arrayInfo, typeInfo),
            PrimitiveType.UInt16 => new JaggedArrayRecord<ushort>(arrayInfo, typeInfo),
            PrimitiveType.UInt32 => new JaggedArrayRecord<uint>(arrayInfo, typeInfo),
            PrimitiveType.UInt64 => new JaggedArrayRecord<ulong>(arrayInfo, typeInfo),
            _ => throw ThrowHelper.InvalidPrimitiveType((PrimitiveType)typeInfo.Infos[0].AdditionalInfo!),
        };
}