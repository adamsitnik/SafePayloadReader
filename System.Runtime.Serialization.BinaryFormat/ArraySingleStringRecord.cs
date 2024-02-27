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
internal sealed class ArraySingleStringRecord : SerializationRecord
{
    private readonly int _id;
    private readonly BinaryObjectStringRecord[] _records;
    private string?[] _values;

    private ArraySingleStringRecord(int objectId, BinaryObjectStringRecord[] records, string?[] values)
    {
        _id = objectId;
        _records = records;
        _values = values;
    }

    public override RecordType RecordType => RecordType.ArraySingleString;

    internal override int Id => _id;

    internal override bool IsFollowedByInlineData => true;

    internal static ArraySingleStringRecord Parse(BinaryReader reader, Dictionary<int, SerializationRecord> recordsMap)
    {
        ArrayInfo arrayInfo = ArrayInfo.Parse(reader);

        BinaryObjectStringRecord[] records = new BinaryObjectStringRecord[arrayInfo.Length];
        string?[] values = new string[arrayInfo.Length];

        for (int i = 0; i < arrayInfo.Length;)
        {
            SerializationRecord record = SafePayloadReader.ReadNext(reader, recordsMap, out RecordType recordType);
            if (recordType == RecordType.BinaryObjectString)
            {
                BinaryObjectStringRecord stringRecord = (BinaryObjectStringRecord)record;
                values[i] = stringRecord.Value;
                records[i++] = stringRecord;
            }
            else if (recordType == RecordType.ObjectNull)
            {
                values[i] = null;
                records[i++] = BinaryObjectStringRecord.NullString;
            }
            else if (recordType is RecordType.ObjectNullMultiple256 or RecordType.ObjectNullMultiple)
            {
                int count = recordType is RecordType.ObjectNullMultiple256
                    ? ((ObjectNullMultiple256Record)record).Count
                    : ((ObjectNullMultipleRecord)record).Count;

                if (i + count > arrayInfo.Length)
                {
                    throw new SerializationException($"Unexpected null object count {count}");
                }

                for (int j = 0; j < count; j++)
                {
                    values[i] = null;
                    records[i++] = BinaryObjectStringRecord.NullString;
                }
            }
            else
            {
                throw new SerializationException($"Unexpected record type {recordType}");
            }
        }

        return new(arrayInfo.ObjectId, records, values);
    }

    public override bool IsSerializedInstanceOf(Type type) => type == typeof(string[]);

    public override object GetValue() => _values;
}