using System.Collections.Generic;
using System.IO;

namespace System.Runtime.Serialization.BinaryFormat;

internal sealed class BinaryArrayRecord : ArrayRecord<ClassRecord?>
{
    internal BinaryArrayRecord(ArrayInfo arrayInfo, BinaryArrayType arrayType, int rank,
        MemberTypeInfo memberTypeInfo, List<SerializationRecord> records) : base(arrayInfo)
    {
        ArrayType = arrayType;
        Rank = rank;
        MemberTypeInfo = memberTypeInfo;
        Records = records;
    }

    public override RecordType RecordType => RecordType.BinaryArray;

    internal BinaryArrayType ArrayType { get; }

    private int Rank { get; }

    private MemberTypeInfo MemberTypeInfo { get; }

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
        List<SerializationRecord> records = new();
        (BinaryType BinaryType, object? AdditionalInfo) = memberTypeInfo.Infos[0];

        int recordsSize = 0;
        while (recordsSize < length)
        {
            SerializationRecord record = (SerializationRecord)ReadValue(reader, recordMap, BinaryType, AdditionalInfo);

            int recordSize = record is NullsRecord nullsRecord ? nullsRecord.NullCount : 1;
            if (recordsSize + recordSize > length)
            {
                throw new SerializationException($"Unexpected Null Record count: {recordSize}.");
            }

            records.Add(record);
            recordsSize += recordSize;
        }

        return new(new ArrayInfo(objectId, length), arrayType, rank, memberTypeInfo, records);
    }
}