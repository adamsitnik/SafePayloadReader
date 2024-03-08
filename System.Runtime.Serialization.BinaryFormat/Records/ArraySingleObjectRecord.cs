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
    private ArraySingleObjectRecord(ArrayInfo arrayInfo)
        : base(arrayInfo)
    {
        Records = new();
        RecordsToRead = arrayInfo.Length;
    }

    public override RecordType RecordType => RecordType.ArraySingleObject;

    internal List<SerializationRecord> Records { get; }

    private int RecordsToRead { get; set; }

    public override bool IsSerializedInstanceOf(Type type) => type == typeof(object[]);

    protected override object?[] Deserialize(bool allowNulls)
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

    internal static ArraySingleObjectRecord Parse(BinaryReader reader)
        => new(ArrayInfo.Parse(reader));

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
}
