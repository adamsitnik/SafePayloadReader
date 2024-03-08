using System.Collections.Generic;
using System.IO;

namespace System.Runtime.Serialization.BinaryFormat;

internal sealed class BinaryArrayRecord : ArrayRecord<ClassRecord?>
{
    internal BinaryArrayRecord(ArrayInfo arrayInfo, BinaryArrayType arrayType, int rank,
        MemberTypeInfo memberTypeInfo) : base(arrayInfo)
    {
        ArrayType = arrayType;
        Rank = rank;
        MemberTypeInfo = memberTypeInfo;
        RecordsToRead = arrayInfo.Length;
        Records = new();
    }

    public override RecordType RecordType => RecordType.BinaryArray;

    internal BinaryArrayType ArrayType { get; }

    private int Rank { get; }

    private MemberTypeInfo MemberTypeInfo { get; }

    private int RecordsToRead { get; set; }

    internal List<SerializationRecord> Records { get; }

    public override bool IsSerializedInstanceOf(Type type)
        => type.IsArray && type.GetArrayRank() == Rank; // TODO: compare the type

    protected override ClassRecord?[] Deserialize(bool allowNulls)
    {
        ClassRecord?[] classRecords = new ClassRecord?[Length];

        for (int recordIndex = 0, valueIndex = 0; recordIndex < Records.Count; recordIndex++)
        {
            SerializationRecord record = Records[recordIndex];

            if (record is MemberReferenceRecord referenceRecord)
            {
                record = referenceRecord.GetReferencedRecord();
            }

            if (record is ClassRecord classRecord)
            {
                classRecords[valueIndex++] = classRecord;
                continue;
            }

            if (!allowNulls)
            {
                throw new SerializationException("The array contained null(s)");
            }

            int nullCount = ((NullsRecord)record).NullCount;
            do
            {
                classRecords[valueIndex++] = null;
                nullCount--;
            } while (nullCount > 0);
        }

        return classRecords;
    }

    internal override object GetValue() => Deserialize();

    internal static BinaryArrayRecord Parse(BinaryReader reader)
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

        return new(new ArrayInfo(objectId, length), arrayType, rank, memberTypeInfo);
    }

    internal override void HandleNextRecord(SerializationRecord nextRecord, NextInfo info)
    {
        RecordsToRead -= nextRecord is NullsRecord nullsRecord ? nullsRecord.NullCount : 1;

        if (RecordsToRead < 0)
        {
            // The only way to get here is to read a multiple null record with Count
            // larger than the number of array items that were left to read.
            ThrowHelper.ThrowUnexpectedNullRecordCount();
        }
        else if (RecordsToRead > 0)
        {
            info.Stack.Push(info);
        }

        Records.Add(nextRecord);
    }

    internal (AllowedRecordTypes allowed, PrimitiveType primitiveType) GetAllowedRecordType()
    {
        (AllowedRecordTypes allowed, PrimitiveType primitiveType) = MemberTypeInfo.GetNextAllowedRecordType(0);

        if (allowed != AllowedRecordTypes.None)
        {
            // It's an array, it can also contain multiple nulls
            return (allowed | AllowedRecordTypes.Nulls, primitiveType);
        }

        return (allowed, primitiveType);
    }
}