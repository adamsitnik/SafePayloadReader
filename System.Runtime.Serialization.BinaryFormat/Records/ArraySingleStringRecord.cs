using System.Collections.Generic;
using System.IO;

namespace System.Runtime.Serialization.BinaryFormat;

/// <summary>
///  Single dimensional array of strings.
/// </summary>
/// <remarks>
///  <para>
///   <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/3d98fd60-d2b4-448a-ac0b-3cd8dea41f9d">
///    [MS-NRBF] 2.4.3.4
///   </see>
///  </para>
/// </remarks>
internal sealed class ArraySingleStringRecord : ArrayRecord<string?>
{
    private ArraySingleStringRecord(ArrayInfo arrayInfo, List<SerializationRecord> records) : base(arrayInfo)
        => Records = records;

    public override RecordType RecordType => RecordType.ArraySingleString;

    private List<SerializationRecord> Records { get; }

    public override bool IsSerializedInstanceOf(Type type) => type == typeof(string[]);

    protected override string?[] Deserialize(bool allowNulls)
    {
        string?[] values = new string?[Length];

        for (int recordIndex = 0, valueIndex = 0; recordIndex < Records.Count; recordIndex++)
        {
            SerializationRecord record = Records[recordIndex];

            if (record is MemberReferenceRecord memberReference)
            {
                record = memberReference.GetReferencedRecord();

                if (record is not BinaryObjectStringRecord)
                {
                    // TODO: consider throwing this exception as soon as we read the referenced record.
                    // It would require registering reference validation checks.
                    throw new SerializationException("The string array contained a reference to non-string.");
                }
            }

            if (record is BinaryObjectStringRecord stringRecord)
            {
                values[valueIndex++] = stringRecord.Value;
                continue;
            }

            if (!allowNulls)
            {
                throw new SerializationException("The array contained null(s).");
            }

            int nullCount = ((NullsRecord)record).NullCount;
            do
            {
                values[valueIndex++] = null;
                nullCount--;
            } while (nullCount > 0);
        }

        return values;
    }

    internal static ArraySingleStringRecord Parse(BinaryReader reader, RecordMap recordsMap)
    {
        ArrayInfo arrayInfo = ArrayInfo.Parse(reader);

        // An array of string can consist of string(s), null(s) and reference(s) to string(s).
        const AllowedRecordTypes allowedTypes = AllowedRecordTypes.BinaryObjectString | AllowedRecordTypes.Nulls | AllowedRecordTypes.MemberReference;

        // We must not pre-allocate an array of given size, as it could be used as a vector of attack.
        // Example: Define a class with 20 string array fields, each of them being an array
        // of max size and containing just a single ObjectNullMultipleRecord record
        // that specifies that the whole array is full of nulls.
        List<SerializationRecord> records = new();

        // BinaryObjectString and ObjectNull has a size == 1, but
        // ObjectNullMultiple256Record and ObjectNullMultipleRecord specify the number of null elements
        // so their size differs.
        int recordsSize = 0;
        while (recordsSize < arrayInfo.Length)
        {
            SerializationRecord record = SafePayloadReader.ReadNextNonRecursive(reader, recordsMap, allowedTypes, out _);

            int recordSize = record is NullsRecord nullsRecord ? nullsRecord.NullCount : 1;
            if (recordsSize + recordSize > arrayInfo.Length)
            {
                throw new SerializationException($"Unexpected Null Record count: {recordSize}.");
            }

            records.Add(record);
            recordsSize += recordSize;
        }

        return new(arrayInfo, records);
    }
}