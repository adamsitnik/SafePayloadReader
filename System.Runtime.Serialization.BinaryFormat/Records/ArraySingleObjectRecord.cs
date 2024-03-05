using System.Collections.Generic;
using System.IO;

namespace System.Runtime.Serialization.BinaryFormat;

/// <summary>
///  Single dimensional array of objects.
/// </summary>
/// <remarks>
///  <para>
///   <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/982b2f50-6367-402a-aaf2-44ee96e2a5e0">
///    [MS-NRBF] 2.4.3.2
///   </see>
///  </para>
/// </remarks>
internal sealed class ArraySingleObjectRecord : ArrayRecord<object?>
{
    private ArraySingleObjectRecord(ArrayInfo arrayInfo, SerializationRecord[] records)
        : base(Map(records))
    {
        ArrayInfo = arrayInfo;
        Records = records;
    }

    public override RecordType RecordType => RecordType.ArraySingleObject;

    internal override int ObjectId => ArrayInfo.ObjectId;

    internal ArrayInfo ArrayInfo { get; }

    internal SerializationRecord[] Records { get; }

    public override bool IsSerializedInstanceOf(Type type) => type == typeof(object[]);

    internal override object GetValue() => Values;

    internal static ArraySingleObjectRecord Parse(BinaryReader reader, RecordMap recordMap)
    {
        ArrayInfo arrayInfo = ArrayInfo.Parse(reader);

        // An array of object can contain any Data, which is everything beside SerializedStreamHeader and MessageEnd.
        const AllowedRecordTypes allowed = AllowedRecordTypes.AnyData;

        // TODO: remove unbounded recursion
        SerializationRecord[] records = ReadRecords(reader, recordMap, arrayInfo.Length, allowed);

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
