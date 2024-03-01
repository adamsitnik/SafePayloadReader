using System.Collections.Generic;
using System.IO;
using System.Text;

namespace System.Runtime.Serialization.BinaryFormat;

public static class SafePayloadReader
{
    public static string ReadString(Stream stream, bool leaveOpen = false)
    {
        var result = (BinaryObjectStringRecord)Read(stream, leaveOpen);
        return result.Value;
    }

    public static ClassRecord ReadClassRecord<T>(Stream stream, bool leaveOpen = false)
        where T : class
    {
        if (typeof(T) == typeof(string))
        {
            throw new ArgumentException("Use ReadString method instead");
        }
        else if (typeof(T).IsArray)
        {
            throw new ArgumentException("Use one of the ReadArray* methods instead");
        }

        var result = (ClassRecord)Read(stream, leaveOpen);
        if (!result.IsSerializedInstanceOf(typeof(T)))
        {
            throw new SerializationException();
        }
        return result;
    }

    /// <exception cref="NotSupportedException">For <seealso cref="System.Half"/> and other primitive types that are not supported by the Binary Formatter itself.</exception>
    public static T ReadPrimitiveType<T>(Stream stream, bool leaveOpen = false)
        where T : unmanaged
    {
        ThrowForUnsupportedPrimitiveType<T>();

        var result = (SystemClassWithMembersAndTypesRecord)Read(stream, leaveOpen);
        if (!result.IsSerializedInstanceOf(typeof(T)))
        {
            throw new SerializationException();
        }
        else if (typeof(T) == typeof(DateTime))
        {
            long raw = (long)result.MemberValues[0]!;
            return (T)(object)SerializationRecord.CreateDateTimeFromData(raw);
        }
        else if (typeof(T) == typeof(TimeSpan))
        {
            long raw = (long)result.MemberValues[0]!;
            return (T)(object)new TimeSpan(raw);
        }
        else if (typeof(T) == typeof(decimal))
        {
            int[] bits =
            [
                (int)result["lo"]!,
                (int)result["mid"]!,
                (int)result["hi"]!,
                (int)result["flags"]!
            ];

            return (T)(object)new decimal(bits);
        }
        else if (typeof (T) == typeof(IntPtr))
        {
            long raw = (long)result.MemberValues[0]!;
            return (T)(object)new IntPtr(raw);
        }
        else if (typeof(T) == typeof(UIntPtr))
        {
            ulong raw = (ulong)result.MemberValues[0]!;
            return (T)(object)new UIntPtr(raw);
        }

        return (T)result.MemberValues[0]!;
    }

    /// <exception cref="NotSupportedException">For <seealso cref="System.Half"/> and other primitive types that are not supported by the Binary Formatter itself.</exception>
    public static T[] ReadArrayOfPrimitiveType<T>(Stream stream, bool leaveOpen = false)
        where T : unmanaged
    {
        ThrowForUnsupportedPrimitiveType<T>();

        var result = (ArraySinglePrimitiveRecord<T>)Read(stream, leaveOpen);
        return result.Values;
    }

    private static void ThrowForUnsupportedPrimitiveType<T>() where T : unmanaged
    {
        // a very weird way of performing typeof(T) == typeof(Half) check in NS2.0
        if (!(typeof(T) == typeof(bool) || typeof(T) == typeof(char)
            || typeof(T) == typeof(byte) || typeof(T) == typeof(sbyte)
            || typeof(T) == typeof(short) || typeof(T) == typeof(ushort)
            || typeof(T) == typeof(int) || typeof(T) == typeof(uint)
            || typeof(T) == typeof(long) || typeof(T) == typeof(ulong)
            || typeof(T) == typeof(IntPtr) || typeof(T) == typeof(UIntPtr)
            || typeof(T) == typeof(float) || typeof(T) == typeof(double)
            || typeof(T) == typeof(decimal)
            || typeof(T) == typeof(DateTime) || typeof(T) == typeof(TimeSpan)))
        {
            throw new NotSupportedException($"Type {typeof(T)} is not supported by the Binary Format.");
        }
    }

    public static string?[] ReadArrayOfStrings(Stream stream, bool leaveOpen = false)
    {
        var result = (ArrayRecord<string?>)Read(stream, leaveOpen);
        return result.Values;
    }

    public static object?[] ReadArrayOfObjects(Stream stream, bool leaveOpen = false)
    {
        var result = (ArrayRecord<object?>)Read(stream, leaveOpen);
        return result.Values;
    }

    public static ClassRecord?[] ReadArrayOfClassRecords(Stream stream, bool leaveOpen = false)
    {
        var result = (ArrayRecord<ClassRecord?>)Read(stream, leaveOpen);
        return result.Values;
    }

    public static SerializationRecord Read(Stream stream, bool leaveOpen = false)
    {
        if (stream is null) throw new ArgumentNullException(nameof(stream));

        using BinaryReader reader = new(stream, Encoding.UTF8, leaveOpen: leaveOpen);
        return Read(reader);
    }

    private static SerializationRecord Read(BinaryReader reader)
    {
        List<SerializationRecord> records = new();
        Dictionary<int, SerializationRecord> recordMap = new();

        RecordType recordType;
        do
        {
            records.Add(ReadNext(reader, recordMap, out recordType));
        } while (recordType != RecordType.MessageEnd);

        return recordMap[1]; // top level record always has ObjectId == 1
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

        // From https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-nrbf/0a192be0-58a1-41d0-8a54-9c91db0ab7bf:
        // "If the ObjectId is not referenced by any MemberReference in the serialization stream,
        // then the ObjectId SHOULD be positive, but MAY be negative."
        if (record.ObjectId != SerializationRecord.NoId)
        {
            // use Add on purpose, so in case of duplicate Ids we get an exception
            recordMap.Add(record.ObjectId, record);
        }

        return record;
    }
}