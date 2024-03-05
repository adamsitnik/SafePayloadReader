using System.Collections.Generic;
using System.IO;

namespace System.Runtime.Serialization.BinaryFormat;

internal sealed class BinaryArrayRecord : ArrayRecord<ClassRecord?>
{
    internal BinaryArrayRecord(ArrayInfo arrayInfo, BinaryArrayType arrayType, int rank,
        MemberTypeInfo memberTypeInfo, ClassRecord?[] records) : base(records)
    {
        ArrayInfo = arrayInfo;
        ArrayType = arrayType;
        Rank = rank;
        MemberTypeInfo = memberTypeInfo;
    }

    public override RecordType RecordType => RecordType.BinaryArray;

    internal override int ObjectId => ArrayInfo.ObjectId;

    internal ArrayInfo ArrayInfo { get; }

    internal BinaryArrayType ArrayType { get; }

    internal int Rank { get; }

    internal MemberTypeInfo MemberTypeInfo { get; }

    public override bool IsSerializedInstanceOf(Type type)
        => type.IsArray && type.GetArrayRank() == Rank; // TODO: compare the type

    internal override object GetValue() => Values;

    internal static BinaryArrayRecord Parse(BinaryReader reader, RecordMap recordMap)
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
        ClassRecord?[] records = new ClassRecord?[length];
        (BinaryType BinaryType, object? AdditionalInfo) = memberTypeInfo.Infos[0];

        for (int i = 0; i < records.Length;)
        {
            object value = ReadValue(reader, recordMap, BinaryType, AdditionalInfo);

            if (value is MemberReferenceRecord referenceRecord)
            {
                value = new LazyClassRecord(referenceRecord);
            }

            if (value is ClassRecord classRecord)
            {
                records[i++] = classRecord;
            }
            else if(Insert(records, ref i, value, null) < 0)
            {
                throw new SerializationException($"Unexpected type: {value.GetType()}");
            }
        }

        return new(new ArrayInfo(objectId, length), arrayType, rank, memberTypeInfo, records);
    }
}