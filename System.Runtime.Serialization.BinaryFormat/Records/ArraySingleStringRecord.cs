using System.Collections.Generic;
using System.IO;
using System.Linq;

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
    private ArraySingleStringRecord(int objectId, List<string?> values) : base(values) => ObjectId = objectId;

    public override RecordType RecordType => RecordType.ArraySingleString;

    internal override int ObjectId { get; }

    public override bool IsSerializedInstanceOf(Type type) => type == typeof(string[]);

    internal override object GetValue() => Values.ToArray();

    internal static ArraySingleStringRecord Parse(BinaryReader reader, RecordMap recordsMap)
    {
        ArrayInfo arrayInfo = ArrayInfo.Parse(reader);

        List<string?> values = new();
        // An array of string can consist of string(s) and null(s)
        AllowedRecordTypes allowedTypes = AllowedRecordTypes.BinaryObjectString | AllowedRecordTypes.Nulls;

        while (values.Count < arrayInfo.Length)
        {
            SerializationRecord record = SafePayloadReader.ReadNext(reader, recordsMap, allowedTypes, out _);

            if (record is BinaryObjectStringRecord stringRecord)
            {
                values.Add(stringRecord.Value);

                allowedTypes = AllowedRecordTypes.BinaryObjectString | AllowedRecordTypes.Nulls;
            }
            else
            {
                Insert(values, arrayInfo.Length, record, null);

                // A null record can not be followed by another null record
                allowedTypes = AllowedRecordTypes.BinaryObjectString;
            }
        }

        return new(arrayInfo.ObjectId, values);
    }
}