using System.Collections.Generic;
using System.IO;
using System.Text;

namespace System.Runtime.Serialization.BinaryFormat;

public static class PayloadReader
{
    private static readonly UTF8Encoding ThrowOnInvalidUtf8Encoding = new(false, throwOnInvalidBytes: true);

    /// <summary>
    /// Reads the provided Binary Format payload.
    /// </summary>
    /// <param name="payload">The Binary Format payload.</param>
    /// <param name="leaveOpen">True to leave the <paramref name="payload"/> payload open
    /// after the reading is finished, otherwise, false.</param>
    /// <returns>A <seealso cref="SerializationRecord"/> that represents the root object.</returns>
    /// <exception cref="ArgumentNullException">When <paramref name="payload"/> is null.</exception>
    /// <exception cref="ArgumentException">The <paramref name="payload"/> payload does not support reading or is already closed.</exception>
    /// <exception cref="SerializationException">When reading input from <paramref name="payload"/> encounters invalid Binary Format data.</exception>
    /// <exception cref="DecoderFallbackException">When reading input from <paramref name="payload"/>
    /// encounters invalid sequence of UTF8 characters.</exception>
    public static SerializationRecord Read(Stream payload, bool leaveOpen = false)
    {
        if (payload is null) throw new ArgumentNullException(nameof(payload));

        using BinaryReader reader = new(payload, ThrowOnInvalidUtf8Encoding, leaveOpen: leaveOpen);
        return Read(reader);
    }

    /// <summary>
    /// Reads the provided Binary Format payload that is expected to contain only a single <seealso cref="string"/>.
    /// </summary>
    /// <returns>The deserialized string value.</returns>
    /// <inheritdoc cref="Read"/>
    public static string ReadString(Stream payload, bool leaveOpen = false)
    {
        var result = (BinaryObjectStringRecord)Read(payload, leaveOpen);
        return result.Value;
    }

    /// <summary>
    /// Reads the provided Binary Format payload that is expected to contain a primitive value of <typeparamref name="T"/> type.
    /// </summary>
    /// <returns>The deserialized <typeparamref name="T"/> value.</returns>
    /// <exception cref="NotSupportedException">For <seealso cref="System.Half"/> and other primitive types that are not natively supported by the Binary Formatter itself.</exception>
    /// <inheritdoc cref="Read"/>
    public static T ReadPrimitiveType<T>(Stream payload, bool leaveOpen = false)
        where T : unmanaged
    {
        ThrowForUnsupportedPrimitiveType<T>();

        var result = (SystemClassWithMembersAndTypesRecord)Read(payload, leaveOpen);
        if (!result.IsTypeNameMatching(typeof(T)))
        {
            ThrowHelper.ThrowTypeMismatch(expected: typeof(T));
        }
        else if (SystemClassWithMembersAndTypesRecord.CanBeMappedToPrimitive<T>())
        {
            return result.GetValue<T>();
        }

        return (T)result.MemberValues[0]!;
    }

    /// <summary>
    /// Reads the provided Binary Format payload that is expected to contain an instance of any class (or struct) that is not an <seealso cref="Array"/> or a primitive type.
    /// </summary>
    /// <returns>A <seealso cref="ClassRecord"/> that represents the root object.</returns>
    /// <inheritdoc cref="Read"/>
    public static ClassRecord ReadAnyClassRecord(Stream payload, bool leaveOpen = false)
        => (ClassRecord)Read(payload, leaveOpen);

    /// <summary>
    /// Reads the provided Binary Format payload that is expected to contain an instance of <typeparamref name="T"/> class (or struct).
    /// </summary>
    /// <returns>A <seealso cref="ClassRecord"/> that represents the root object.</returns>
    /// <remarks><typeparamref name="T"/> needs to be the exact type, not a base type or an abstraction.</remarks>
    /// <inheritdoc cref="Read"/>
    public static ClassRecord ReadExactClassRecord<T>(Stream payload, bool leaveOpen = false)
    {
        ClassRecord result = ReadAnyClassRecord(payload, leaveOpen);
        if (!result.IsTypeNameMatching(typeof(T)))
        {
            ThrowHelper.ThrowTypeMismatch(expected: typeof(T));
        }
        return result;
    }

    /// <summary>
    /// Reads the provided Binary Format payload that is expected to contain an instance
    /// of any array (single dimension, jagged or multi-dimension).
    /// </summary>
    /// <returns>An <seealso cref="ArrayRecord"/> that represents the root object.</returns>
    /// <inheritdoc cref="Read"/>
    public static ArrayRecord ReadAnyArrayRecord(Stream payload, bool leaveOpen = false)
        => (ArrayRecord)Read(payload, leaveOpen);

    /// <summary>
    /// Reads the provided Binary Format payload that is expected to contain a single dimension array of primitive values of <typeparamref name="T"/> type.
    /// </summary>
    /// <returns>The deserialized array of <typeparamref name="T"/>.</returns>
    /// <exception cref="NotSupportedException">For <seealso cref="System.Half"/> and other primitive types that are not natively supported by the Binary Formatter itself.</exception>
    /// <inheritdoc cref="Read"/>
    public static T[] ReadArrayOfPrimitiveType<T>(Stream payload, bool leaveOpen = false)
        where T : unmanaged
    {
        ThrowForUnsupportedPrimitiveType<T>();

        var result = (ArrayRecord<T>)Read(payload, leaveOpen);
        return result.ToArray();
    }

    /// <summary>
    /// Reads the provided Binary Format payload that is expected to contain a single dimension array of <seealso cref="string"/>.
    /// </summary>
    /// <param name="allowNulls">True to allow for null values, otherwise, false.</param>
    /// <returns>The deserialized array of <seealso cref="string"/>.</returns>
    /// <inheritdoc cref="Read"/>
    public static string?[] ReadArrayOfStrings(Stream payload, bool leaveOpen = false, bool allowNulls = true)
    {
        var result = (ArrayRecord<string>)Read(payload, leaveOpen);
        return result.ToArray(allowNulls);
    }

    /// <summary>
    /// Reads the provided Binary Format payload that is expected to contain a single dimension array of <seealso cref="object"/>.
    /// </summary>
    /// <param name="allowNulls">True to allow for null values, otherwise, false.</param>
    /// <returns>The deserialized array of <seealso cref="object"/>.</returns>
    /// <remarks>
    /// <para>Only primitive types and nulls are deserialized to their raw values.</para>
    /// <para>For other types that are not arrays, elements are represented as <seealso cref="ClassRecord"/> instances.</para>
    /// <para>For jagged and multi-dimensional arrays, elements are represented as instances of <seealso cref="ArrayRecord"/>.</para></remarks>
    /// <inheritdoc cref="Read"/>
    public static object?[] ReadArrayOfObjects(Stream payload, bool leaveOpen = false, bool allowNulls = true)
    {
        var result = (ArrayRecord<object>)Read(payload, leaveOpen);
        return result.ToArray(allowNulls);
    }

    /// <summary>
    /// Reads the provided Binary Format payload that is expected to contain a single dimension array of any class (or struct) instances.
    /// </summary>
    /// <returns>An array of <seealso cref="ClassRecord"/> instances.</returns>
    /// <inheritdoc cref="Read"/>
    public static ClassRecord?[] ReadArrayOfAnyClassRecords(Stream stream, bool leaveOpen = false, bool allowNulls = true)
    {
        var result = (ArrayRecord<ClassRecord>)Read(stream, leaveOpen);
        return result.ToArray(allowNulls);
    }

    /// <summary>
    /// Reads the provided Binary Format payload that is expected to contain a single dimension array of <typeparamref name="T"/> class (or struct) instances.
    /// </summary>
    /// <returns>An array of <seealso cref="ClassRecord"/> instances.</returns>
    /// <inheritdoc cref="Read"/>
    public static ClassRecord?[] ReadArrayOfExactClassRecords<T>(Stream stream, bool leaveOpen = false, bool allowNulls = true)
    {
        var result = (ArrayRecord<ClassRecord>)Read(stream, leaveOpen);
        if (!result.IsElementType(typeof(T)))
        {
            ThrowHelper.ThrowTypeMismatch(expected: typeof(T[]));
        }
        return result.ToArray(allowNulls);
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
}