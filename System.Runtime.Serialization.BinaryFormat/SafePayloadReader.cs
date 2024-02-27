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
        //ArgumentNullException.ThrowIfNull(stream);

        using BinaryReader reader = new(stream, Encoding.UTF8, leaveOpen: leaveOpen);
        return Read(reader);
    }

    public static SafePayloadReader Read(BinaryReader reader)
    {
        //ArgumentNullException.ThrowIfNull(reader);

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
            RecordType.SerializedStreamHeader => SerializedStreamHeaderRecord.Parse(reader),
            RecordType.BinaryLibrary => BinaryLibraryRecord.Parse(reader),
            RecordType.MessageEnd => MessageEndRecord.Parse(),
            RecordType.ClassWithMembersAndTypes => ClassWithMembersAndTypesRecord.Parse(reader, recordMap),
            RecordType.BinaryObjectString => BinaryObjectStringRecord.Parse(reader),
            _ => null! // throw new NotImplementedException()
        };

        if (record?.Id >= 0)
        {
            recordMap.Add(record.Id, record);
        }

        return record;
    }
}