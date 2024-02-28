using System.Collections.Generic;
using System.IO;
using System.Text;

namespace System.Runtime.Serialization.BinaryFormat;

public class SafePayloadReader
{
    private readonly List<SerializationRecord> _records;

    private SafePayloadReader(List<SerializationRecord> records) => _records = records;

    public IReadOnlyList<SerializationRecord> Records => _records;

    public static SafePayloadReader Read(Stream stream, bool leaveOpen = false)
    {
        if (stream is null) throw new ArgumentNullException(nameof(stream));

        using BinaryReader reader = new(stream, Encoding.UTF8, leaveOpen: leaveOpen);
        return Read(reader);
    }

    public static SafePayloadReader Read(BinaryReader reader)
    {
        if (reader is null) throw new ArgumentNullException(nameof(reader));

        List<SerializationRecord> records = new();
        Dictionary<int, SerializationRecord> recordMap = new();

        RecordType recordType;
        do
        {
            records.Add(ReadNext(reader, recordMap, out recordType));
        } while (recordType != RecordType.MessageEnd);

        return new(records);
    }

    internal static SerializationRecord ReadNext(BinaryReader reader, Dictionary<int, SerializationRecord> recordMap, out RecordType recordType)
    {
        recordType = (RecordType)reader.ReadByte();

        SerializationRecord record = recordType switch
        {
            RecordType.ArraySingleObject => ArraySingleObjectRecord.Parse(reader, recordMap),
            RecordType.ArraySinglePrimitive => ArraySinglePrimitiveRecord<int>.Parse(reader),
            RecordType.ArraySingleString => ArraySingleStringRecord.Parse(reader, recordMap),
            RecordType.BinaryArray => BinaryArrayRecord.Parse(reader, recordMap),
            RecordType.BinaryLibrary => BinaryLibraryRecord.Parse(reader),
            RecordType.BinaryObjectString => BinaryObjectStringRecord.Parse(reader),
            RecordType.ClassWithId => ClassWithIdRecord.Parse(reader, recordMap),
            RecordType.ClassWithMembers => ClassWithMembersRecord.Parse(reader, recordMap),
            RecordType.ClassWithMembersAndTypes => ClassWithMembersAndTypesRecord.Parse(reader, recordMap),
            RecordType.MemberPrimitiveTyped => MemberPrimitiveTypedRecord.Parse(reader),
            RecordType.MemberReference => MemberReferenceRecord.Parse(reader, recordMap),
            RecordType.MessageEnd => MessageEndRecord.Singleton,
            RecordType.ObjectNull => ObjectNullRecord.Instance,
            RecordType.ObjectNullMultiple => ObjectNullMultipleRecord.Parse(reader),
            RecordType.ObjectNullMultiple256 => ObjectNullMultiple256Record.Parse(reader),
            RecordType.SerializedStreamHeader => SerializedStreamHeaderRecord.Parse(reader),
            RecordType.SystemClassWithMembers => SystemClassWithMembersRecord.Parse(reader, recordMap),
            RecordType.SystemClassWithMembersAndTypes => SystemClassWithMembersAndTypesRecord.Parse(reader, recordMap),
            _ => throw new NotSupportedException("Remote invocation is not supported by design")
        };

        if (record.Id >= 0)
        {
            recordMap.Add(record.Id, record);
        }

        return record;
    }

    public SerializationRecord GetTopLevel<T>()
    {
        foreach (SerializationRecord record in Records)
        {
            if (record.IsSerializedInstanceOf(typeof(T)))
            {
                return record;
            }
        }

        throw new KeyNotFoundException();
    }
}