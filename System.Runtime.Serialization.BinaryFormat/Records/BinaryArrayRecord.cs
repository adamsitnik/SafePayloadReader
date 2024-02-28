using System.Collections.Generic;
using System.IO;

namespace System.Runtime.Serialization.BinaryFormat;

internal sealed class BinaryArrayRecord : SerializationRecord
{
    internal BinaryArrayRecord(ArrayInfo arrayInfo, BinaryArrayType arrayType, int rank,
        MemberTypeInfo memberTypeInfo, object[] arrayObjects)
    {
        ArrayInfo = arrayInfo;
        ArrayType = arrayType;
        Rank = rank;
        MemberTypeInfo = memberTypeInfo;
        ArrayObjects = arrayObjects;
    }

    public override RecordType RecordType => RecordType.BinaryArray;
    internal override int Id => ArrayInfo.ObjectId;
    internal ArrayInfo ArrayInfo { get; }
    internal BinaryArrayType ArrayType { get; }
    internal int Rank { get; }
    internal MemberTypeInfo MemberTypeInfo { get; }
    internal object[] ArrayObjects { get; }
    private object?[]? Values { get; set; }

    public override bool IsSerializedInstanceOf(Type type)
        => type.IsArray; // TODO: compare the type

    public override object GetValue()
    {
        if (Values is null)
        {
            object?[] values = new object[ArrayObjects.Length];
            for (int i = 0; i < ArrayObjects.Length; i++)
            {
                object? value = ArrayObjects[i];

                if (value is SerializationRecord record)
                {
                    value = record.GetValue();
                }

                values[i] = value;
            }
            Values = values;
        }

        return Values;
    }

    internal static BinaryArrayRecord Parse(BinaryReader reader, Dictionary<int, SerializationRecord> recordMap)
    {
        int objectId = reader.ReadInt32();
        BinaryArrayType arrayType = (BinaryArrayType)reader.ReadByte();
        int rank = reader.ReadInt32();
        int length = reader.ReadInt32();

        if (arrayType != BinaryArrayType.Single || rank != 1)
        {
            throw new NotSupportedException("Only single dimensional arrays are currently supported.");
        }

        MemberTypeInfo memberTypeInfo = MemberTypeInfo.Parse(reader, 1);
        object[] arrayObjects = new object[length];
        (BinaryType BinaryType, object? AdditionalInfo) = memberTypeInfo.Infos[0];

        for (int i = 0; i < arrayObjects.Length;)
        {
            object value = ReadValue(reader, recordMap, BinaryType, AdditionalInfo);

            Insert(arrayObjects, ref i, value, ObjectNullRecord.Instance);
        }

        return new(new ArrayInfo(objectId, length), arrayType, rank, memberTypeInfo, arrayObjects);
    }
}