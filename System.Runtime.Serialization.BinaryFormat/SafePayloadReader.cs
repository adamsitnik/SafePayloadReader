using System.Collections.Generic;
using System.IO;
using System.Text;

namespace System.Runtime.Serialization.BinaryFormat;

public static class SafePayloadReader
{
    private static readonly UTF8Encoding ThrowOnInvalidUtf8Encoding = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

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
        else if (SystemClassWithMembersAndTypesRecord.CanBeMappedToPrimitive<T>())
        {
            return result.GetValue<T>();
        }

        return (T)result.MemberValues[0]!;
    }

    /// <exception cref="NotSupportedException">For <seealso cref="System.Half"/> and other primitive types that are not supported by the Binary Formatter itself.</exception>
    public static T[] ReadArrayOfPrimitiveType<T>(Stream stream, bool leaveOpen = false)
        where T : unmanaged
    {
        ThrowForUnsupportedPrimitiveType<T>();

        var result = (ArrayRecord<T>)Read(stream, leaveOpen);
        return result.ToArray();
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

    public static string?[] ReadArrayOfStrings(Stream stream, bool leaveOpen = false, bool allowNulls = true)
    {
        var result = (ArrayRecord<string>)Read(stream, leaveOpen);
        return result.ToArray(allowNulls);
    }

    public static object?[] ReadArrayOfObjects(Stream stream, bool leaveOpen = false, bool allowNulls = true)
    {
        var result = (ArrayRecord<object>)Read(stream, leaveOpen);
        return result.ToArray(allowNulls);
    }

    public static ClassRecord?[] ReadArrayOfClassRecords(Stream stream, bool leaveOpen = false, bool allowNulls = true)
    {
        var result = (ArrayRecord<ClassRecord>)Read(stream, leaveOpen);
        return result.ToArray(allowNulls);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="leaveOpen"></param>
    /// <returns>Top level object.</returns>
    /// <exception cref="ArgumentNullException">When <paramref name="stream"/> is null.</exception>
    /// <exception cref="DecoderFallbackException">When reading input from <paramref name="stream"/> encounters invalid sequence of UTF8 characters.</exception>
    public static SerializationRecord Read(Stream stream, bool leaveOpen = false)
    {
        if (stream is null) throw new ArgumentNullException(nameof(stream));

        using BinaryReader reader = new(stream, ThrowOnInvalidUtf8Encoding, leaveOpen: leaveOpen);
        return Read(reader);
    }

    private static SerializationRecord Read(BinaryReader reader)
    {
        Stack<NextInfo> readStack = new();
        RecordMap recordMap = new();

        // Everything has to start with a header
        var header = (SerializedStreamHeaderRecord)ReadNext(reader, recordMap, AllowedRecordTypes.SerializedStreamHeader, out _);
        // and can be followed by any Object, BinaryLibrary and a MessageEnd.
        const AllowedRecordTypes allowed = AllowedRecordTypes.AnyObject
            | AllowedRecordTypes.BinaryLibrary | AllowedRecordTypes.MessageEnd;

        RecordType recordType;
        SerializationRecord nextRecord;
        do
        {
            while (readStack.Count > 0)
            {
                NextInfo nextInfo = readStack.Pop();

                if (nextInfo.Allowed != AllowedRecordTypes.None) 
                {
                    // Read the next Record.
                    nextRecord = ReadNext(reader, recordMap, nextInfo.Allowed, out _);
                    // Handle it:
                    // - add to the parent records list,
                    // - push next info if there are remaining nested records to read.
                    nextInfo.Parent.HandleNextRecord(nextRecord, nextInfo);
                    // Push on the top of the stack the first nested record of last read record,
                    // so it gets read as next record.
                    PushFirstNestedRecordInfo(nextRecord, readStack);
                }
                else
                {
                    object value = reader.ReadPrimitiveType(nextInfo.PrimitiveType);

                    nextInfo.Parent.HandleNextValue(value, nextInfo);
                }
            }

            nextRecord = ReadNext(reader, recordMap, allowed, out recordType);
            PushFirstNestedRecordInfo(nextRecord, readStack);
        } while (recordType != RecordType.MessageEnd);

        return recordMap[header.RootId];
    }

    private static SerializationRecord ReadNext(BinaryReader reader, RecordMap recordMap, 
        AllowedRecordTypes allowed, out RecordType recordType)
    {
        recordType = (RecordType)reader.ReadByte();

        if (((uint)allowed & (1u << (int)recordType)) == 0)
        {
            throw new SerializationException($"Unexpected type seen: {recordType}.");
        }

        SerializationRecord record = recordType switch
        {
            RecordType.ArraySingleObject => ArraySingleObjectRecord.Parse(reader),
            RecordType.ArraySinglePrimitive => ArraySinglePrimitiveRecord<int>.Parse(reader),
            RecordType.ArraySingleString => ArraySingleStringRecord.Parse(reader),
            RecordType.BinaryArray => BinaryArrayRecord<ClassRecord>.Parse(reader),
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
            RecordType.SystemClassWithMembers => SystemClassWithMembersRecord.Parse(reader),
            RecordType.SystemClassWithMembersAndTypes => SystemClassWithMembersAndTypesRecord.Parse(reader),
            RecordType.CrossAppDomainAssembly or RecordType.CrossAppDomainMap or RecordType.CrossAppDomainString
                => throw new NotSupportedException("Cross domain is not supported by design"),
            RecordType.MethodCall or RecordType.MethodReturn
                => throw new NotSupportedException("Remote invocation is not supported by design"),
            _ => throw new SerializationException($"Invalid RecordType value: {recordType}")
        };

        recordMap.Add(record);

        return record;
    }

    /// <summary>
    /// This method is responsible for pushing only the FIRST read info 
    /// of the NESTED record into the <paramref name="readStack"/>.
    /// It's not pushing all of them, because it could be used as a vector of attack.
    /// Example: BinaryArrayRecord with <seealso cref="Array.MaxLength"/> length,
    /// where first item turns out to be <seealso cref="ObjectNullMultipleRecord"/>
    /// that provides <seealso cref="Array.MaxLength"/> nulls.
    /// </summary>
    private static void PushFirstNestedRecordInfo(SerializationRecord record, Stack<NextInfo> readStack)
    {
        if (record is ClassRecord classRecord)
        {
            if (classRecord.ExpectedValuesCount > 0)
            {
                (AllowedRecordTypes allowed, PrimitiveType primitiveType) = classRecord.GetNextAllowedRecordType();

                readStack.Push(new(allowed, classRecord, readStack, primitiveType));
            }
        }
        else if (record is ArrayRecord arrayRecord && arrayRecord.ValuesToRead > 0)
        {
            (AllowedRecordTypes allowed, PrimitiveType primitiveType) = arrayRecord.GetAllowedRecordType();

            readStack.Push(new(allowed, arrayRecord, readStack, primitiveType));
        }
    }
}