using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Runtime.Serialization.BinaryFormat;

public sealed class RectangularArrayRecord : ArrayRecord
{
    private RectangularArrayRecord(Type elementType, ArrayInfo arrayInfo, MemberTypeInfo memberTypeInfo,
        int[] lengths, int[] offsets) : base(arrayInfo)
    {
        ElementType = elementType;
        MemberTypeInfo = memberTypeInfo;
        Lengths = lengths;
        Offsets = offsets;
        Values = new();
    }

    public override RecordType RecordType => RecordType.BinaryArray;

    private Type ElementType { get; }

    private MemberTypeInfo MemberTypeInfo { get; }

    private int[] Lengths { get; }

    private int[] Offsets { get; }

    // This is the only array type that may have more elements than Array.MaxLength,
    // that is why we use Linked instead of regular List here.
    internal LinkedList<object> Values { get; }

    public override bool IsSerializedInstanceOf(Type type)
    {
        if (!type.IsArray || type.GetArrayRank() != ArrayInfo.Rank)
        {
            return false;
        }

        Type typeElement = type.GetElementType()!;
        while (typeElement.IsArray)
        {
            typeElement = typeElement.GetElementType()!;
        }

        // TODO: handle case when ElementType is typeof(ClassRecord))
        if (typeElement != ElementType)
        {
            return false;
        }

        if (ArrayInfo.ArrayType == BinaryArrayType.Rectangular)
        {
            return true;
        }

        // TODO: find a cheaper way to compare offsets (there seems to be no reflection API for that)
        // We don't use actual lengths, as it could allocate a lot of memory and be used as a vector of attack.
        int[] lengths = new int[Lengths.Length];
        return Array.CreateInstance(ElementType, lengths, Offsets).GetType() == type;
    }

    public Array Deserialize(bool allowNulls = true, int maxLength = 64_000)
    {
        if (Length > maxLength)
        {
            ThrowHelper.ThrowMaxArrayLength(maxLength, Length);
        }

        Array result = Array.CreateInstance(ElementType, Lengths, Offsets);

#if NET6_0_OR_GREATER
        // I took this idea from Array.CoreCLR that maps an array of int indices into
        // an internal flat index.
        // Yes, I know I'll most likely burn in hell for doing that.
        if (ElementType.IsValueType)
        {
            if (ElementType == typeof(bool)) CopyTo<bool>(Values, result);
            else if (ElementType == typeof(byte)) CopyTo<byte>(Values, result);
            else if (ElementType == typeof(sbyte)) CopyTo<sbyte>(Values, result);
            else if (ElementType == typeof(short)) CopyTo<short>(Values, result);
            else if (ElementType == typeof(ushort)) CopyTo<ushort>(Values, result);
            else if (ElementType == typeof(char)) CopyTo<char>(Values, result);
            else if (ElementType == typeof(int)) CopyTo<int>(Values, result);
            else if (ElementType == typeof(float)) CopyTo<float>(Values, result);
            else if (ElementType == typeof(long)) CopyTo<long>(Values, result);
            else if (ElementType == typeof(ulong)) CopyTo<ulong>(Values, result);
            else if (ElementType == typeof(double)) CopyTo<double>(Values, result);
            else if (ElementType == typeof(TimeSpan)) CopyTo<TimeSpan>(Values, result);
            else if (ElementType == typeof(DateTime)) CopyTo<DateTime>(Values, result);
            else if (ElementType == typeof(decimal)) CopyTo<decimal>(Values, result);
        }
        else
        {
            ref byte arrayDataRef = ref MemoryMarshal.GetArrayDataReference(result);
            ref object elementRef = ref Unsafe.As<byte, object>(ref arrayDataRef);
            nuint flattenedIndex = 0;
            foreach (object value in Values)
            {
                ref object offsetElementRef = ref Unsafe.Add(ref elementRef, flattenedIndex);
                offsetElementRef = GetActualValue(value)!;
                flattenedIndex++;
            }
        }
#endif

        return result;
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

        Values.AddLast(value);
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

    internal static RectangularArrayRecord Create(ArrayInfo arrayInfo, MemberTypeInfo memberTypeInfo, int[] lengths, int[] offsets)
    {
        return memberTypeInfo.Infos[0].BinaryType switch
        {
            BinaryType.Primitive => new(MapPrimitive((PrimitiveType)memberTypeInfo.Infos[0].AdditionalInfo!), arrayInfo, memberTypeInfo, lengths, offsets),
            BinaryType.String => new(typeof(string), arrayInfo, memberTypeInfo, lengths, offsets),
            BinaryType.Object => new(typeof(object), arrayInfo, memberTypeInfo, lengths, offsets),
            BinaryType.SystemClass or BinaryType.Class => new(typeof(ClassRecord), arrayInfo, memberTypeInfo, lengths, offsets),
            _ => throw ThrowHelper.InvalidBinaryType(memberTypeInfo.Infos[0].BinaryType),
        };
    }

    private static Type MapPrimitive(PrimitiveType primitiveType)
        => primitiveType switch
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
            _ => throw ThrowHelper.InvalidPrimitiveType(primitiveType),
        };

#if NET6_0_OR_GREATER
    private static void CopyTo<T>(LinkedList<object> list, Array array) where T : unmanaged
    {
        ref byte arrayDataRef = ref MemoryMarshal.GetArrayDataReference(array);
        ref T elementRef = ref Unsafe.As<byte, T>(ref arrayDataRef);
        nuint flattenedIndex = 0;
        foreach (object value in list)
        {
            ref T targetIndex = ref Unsafe.Add(ref elementRef, flattenedIndex);
            targetIndex = (T)GetActualValue(value)!;
            flattenedIndex++;
        }
    }
#endif

    private static object? GetActualValue(object value)
        => value is SerializationRecord serializationRecord
            ? serializationRecord.GetValue()
            : value; // it must be a primitive type
}
