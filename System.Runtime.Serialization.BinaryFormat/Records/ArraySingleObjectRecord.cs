using System.Collections.Generic;
using System.IO;

namespace System.Runtime.Serialization.BinaryFormat;

internal sealed class ArraySingleObjectRecord : SerializationRecord
{
    private ArraySingleObjectRecord(ArrayInfo arrayInfo, SerializationRecord[] records)
    {
        ArrayInfo = arrayInfo;
        Records = records;
    }

    public override RecordType RecordType => RecordType.ArraySingleObject;
    internal override int Id => ArrayInfo.ObjectId;
    internal ArrayInfo ArrayInfo { get; }
    internal SerializationRecord[] Records { get; }
    private object?[]? Values { get; set; }

    public override bool IsSerializedInstanceOf(Type type) => type == typeof(object[]);

    public override object GetValue()
    {
        if (Values is null)
        {
            object?[] values = new object?[Records.Length];
            for (int i = 0; i < Records.Length; i++)
            {
                values[i] = Records[i].GetValue();
            }
            Values = values;
        }

        return Values;
    }

    internal static ArraySingleObjectRecord Parse(BinaryReader reader, Dictionary<int, SerializationRecord> recordMap)
    {
        ArrayInfo arrayInfo = ArrayInfo.Parse(reader);
        SerializationRecord[] records = ReadRecords(reader, recordMap, arrayInfo.Length);

        return new(arrayInfo, records);
    }
}
