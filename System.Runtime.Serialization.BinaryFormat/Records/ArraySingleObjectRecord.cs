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
    private ArraySingleObjectRecord(ArrayInfo arrayInfo, List<SerializationRecord> records)
        : base(arrayInfo)
    {
        Records = records;
    }

    public override RecordType RecordType => RecordType.ArraySingleObject;

    private List<SerializationRecord> Records { get; }

    public override bool IsSerializedInstanceOf(Type type) => type == typeof(object[]);

    public override object?[] Deserialize(bool allowNulls = true)
    {
        object?[] values = new object?[Length];

        for (int recordIndex = 0, valueIndex = 0; recordIndex < Records.Count; recordIndex++)
        {
            SerializationRecord record = Records[recordIndex];

            int nullCount = record is NullsRecord nullsRecord ? nullsRecord.NullCount : 0;
            if (nullCount == 0)
            {
                values[valueIndex++] = record is MemberReferenceRecord referenceRecord && referenceRecord.Reference == ObjectId
                    ? values // a reference to self, and a way to get StackOverflow exception ;)
                    : record.GetValue();
                continue;
            }

            if (!allowNulls)
            {
                throw new SerializationException("The array contained null(s)");
            }

            do
            {
                values[valueIndex++] = null;
                nullCount--;
            } while (nullCount > 0);
        }

        return values;
    }

    internal override object GetValue() => Deserialize();

    internal static ArraySingleObjectRecord Parse(BinaryReader reader, RecordMap recordMap)
    {
        ArrayInfo arrayInfo = ArrayInfo.Parse(reader);

        // An array of object can contain any Data, which is everything beside SerializedStreamHeader and MessageEnd.
        const AllowedRecordTypes allowed = AllowedRecordTypes.AnyData;

        // TODO: remove unbounded recursion
        List<SerializationRecord> records = ReadRecords(reader, recordMap, arrayInfo.Length, allowed);

        return new(arrayInfo, records);
    }
}
