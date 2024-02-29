using System.Collections.Generic;
using System.IO;

namespace System.Runtime.Serialization.BinaryFormat;

internal sealed class ArraySingleObjectRecord : ArrayRecord<object?>
{
    private ArraySingleObjectRecord(ArrayInfo arrayInfo, SerializationRecord[] records)
        : base(Map(records))
    {
        ArrayInfo = arrayInfo;
        Records = records;
    }

    public override RecordType RecordType => RecordType.ArraySingleObject;

    internal override int Id => ArrayInfo.ObjectId;

    internal ArrayInfo ArrayInfo { get; }

    internal SerializationRecord[] Records { get; }

    public override bool IsSerializedInstanceOf(Type type) => type == typeof(object[]);

    internal override object GetValue() => Values;

    internal static ArraySingleObjectRecord Parse(BinaryReader reader, Dictionary<int, SerializationRecord> recordMap)
    {
        ArrayInfo arrayInfo = ArrayInfo.Parse(reader);
        SerializationRecord[] records = ReadRecords(reader, recordMap, arrayInfo.Length);

        return new(arrayInfo, records);
    }

    private static object?[] Map(SerializationRecord[] records)
    {
        object?[] values = new object?[records.Length];
        for (int i = 0; i < records.Length; i++)
        {
            values[i] = records[i].GetValue();
        }
        return values;
    }
}
