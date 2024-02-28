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
    private readonly BinaryObjectStringRecord[] _records;
    private string?[]? _values;

    private ArraySingleStringRecord(int objectId, BinaryObjectStringRecord[] records)
    {
        Id = objectId;
        _records = records;
    }

    public override RecordType RecordType => RecordType.ArraySingleString;

    internal override int Id { get; }

    public override bool IsSerializedInstanceOf(Type type) => type == typeof(string[]);

    public override object GetValue()
    {
        if (_values is null)
        {
            string?[] values = new string?[_records.Length];
            for (int i = 0; i < _records.Length; i++)
            {
                values[i] = _records[i].Value;
            }
            _values = values;
        }

        return _values;
    }

    internal static ArraySingleStringRecord Parse(BinaryReader reader, Dictionary<int, SerializationRecord> recordsMap)
    {
        ArrayInfo arrayInfo = ArrayInfo.Parse(reader);

        BinaryObjectStringRecord[] records = new BinaryObjectStringRecord[arrayInfo.Length];

        for (int i = 0; i < arrayInfo.Length;)
        {
            SerializationRecord record = SafePayloadReader.ReadNext(reader, recordsMap, out RecordType recordType);

            if (record is BinaryObjectStringRecord stringRecord)
            {
                records[i++] = stringRecord;
            }
            else if (Insert(records, ref i, record, BinaryObjectStringRecord.NullString) < 0)
            {
                throw new SerializationException($"Unexpected record type {recordType}");
            }
        }

        return new(arrayInfo.ObjectId, records);
    }
}