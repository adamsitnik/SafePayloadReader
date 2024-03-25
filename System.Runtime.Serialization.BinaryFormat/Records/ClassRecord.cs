using System.Collections.Generic;
using System.Diagnostics;

namespace System.Runtime.Serialization.BinaryFormat;

/// <summary>
///  Base class for class records.
/// </summary>
/// <remarks>
///  <para>
///   Includes the values for the class (which trail the record)
///   <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/c9bc3af3-5a0c-4b29-b517-1b493b51f7bb">
///    [MS-NRBF] 2.3
///   </see>.
///  </para>
/// </remarks>
public abstract class ClassRecord : SerializationRecord
{
    private const int MaxLength = ArrayRecord.DefaultMaxArrayLength;

    private protected ClassRecord(ClassInfo classInfo)
    {
        ClassInfo = classInfo;
        MemberValues = new();
    }

    public string TypeName => ClassInfo.Name;

    public abstract string LibraryName { get; }

    // Currently we don't expose raw values, so we are not preserving the order here.
    public IEnumerable<string> MemberNames => ClassInfo.MemberNames.Keys;

    internal override int ObjectId => ClassInfo.ObjectId;

    internal abstract int ExpectedValuesCount { get; }

    internal ClassInfo ClassInfo { get; }

    internal List<object?> MemberValues { get; }

    /// <summary>
    /// Retrieves the value of the provided <paramref name="memberName"/>.
    /// </summary>
    /// <param name="memberName">The name of the field.</param>
    /// <returns>The value.</returns>
    /// <exception cref="KeyNotFoundException">Member of such name does not exist.</exception>
    /// <exception cref="InvalidOperationException">Member of such name has value of a different type.</exception>
    public ClassRecord? GetClassRecord(string memberName) => GetMember<ClassRecord>(memberName);

    /// <inheritdoc cref="GetClassRecord(string)"/>
    public string? GetString(string memberName) => GetMember<string>(memberName);
    /// <inheritdoc cref="GetClassRecord(string)"/>
    public bool GetBoolean(string memberName) => GetMember<bool>(memberName);
    /// <inheritdoc cref="GetClassRecord(string)"/>
    public byte GetByte(string memberName) => GetMember<byte>(memberName);
    /// <inheritdoc cref="GetClassRecord(string)"/>
    public sbyte GetSByte(string memberName) => GetMember<sbyte>(memberName);
    /// <inheritdoc cref="GetClassRecord(string)"/>
    public short GetInt16(string memberName) => GetMember<short>(memberName);
    /// <inheritdoc cref="GetClassRecord(string)"/>
    public ushort GetUInt16(string memberName) => GetMember<ushort>(memberName);
    /// <inheritdoc cref="GetClassRecord(string)"/>
    public char GetChar(string memberName) => GetMember<char>(memberName);
    /// <inheritdoc cref="GetClassRecord(string)"/>
    public int GetInt32(string memberName) => GetMember<int>(memberName);
    /// <inheritdoc cref="GetClassRecord(string)"/>
    public uint GetUInt32(string memberName) => GetMember<uint>(memberName);
    /// <inheritdoc cref="GetClassRecord(string)"/>
    public float GetSingle(string memberName) => GetMember<float>(memberName);
    /// <inheritdoc cref="GetClassRecord(string)"/>
    public long GetInt64(string memberName) => GetMember<long>(memberName);
    /// <inheritdoc cref="GetClassRecord(string)"/>
    public ulong GetUInt64(string memberName) => GetMember<ulong>(memberName);
    /// <inheritdoc cref="GetClassRecord(string)"/>
    public double GetDouble(string memberName) => GetMember<double>(memberName);
    /// <inheritdoc cref="GetClassRecord(string)"/>
    public decimal GetDecimal(string memberName) => GetMember<decimal>(memberName);
    /// <inheritdoc cref="GetClassRecord(string)"/>
    public TimeSpan GetTimeSpan(string memberName) => GetMember<TimeSpan>(memberName);
    /// <inheritdoc cref="GetClassRecord(string)"/>
    public DateTime GetDateTime(string memberName) => GetMember<DateTime>(memberName);

    /// <returns>
    /// <para>For primitive types like <seealso cref="int"/>, <seealso cref="string"/> or <seealso cref="DateTime"/> returns their value.</para>
    /// <para>For nulls, returns a null.</para>
    /// <para>For other types that are not arrays, returns an instance of <seealso cref="ClassRecord"/>.</para>
    /// <para>For single-dimensional arrays returns <seealso cref="ArrayRecord{T}"/> where the generic type is the primitive type or <seealso cref="ClassRecord"/>.</para>
    /// <para>For jagged and multi-dimensional arrays, returns an instance of <seealso cref="ArrayRecord"/>.</para>
    /// </returns>
    /// <inheritdoc cref="GetClassRecord(string)"/>
    public object? GetObject(string memberName) => GetMember<object>(memberName);

    /// <summary>
    /// Retrieves an array for the provided <paramref name="memberName"/>.
    /// </summary>
    /// <param name="memberName">The name of the field.</param>
    /// <param name="allowNulls">Specifies whether null values are allowed.</param>
    /// <param name="maxLength">Specifies the max length of an array that can be allocated.</param>
    /// <returns>The array itself or null.</returns>
    /// <exception cref="KeyNotFoundException">Member of such name does not exist.</exception>
    /// <exception cref="InvalidOperationException">Member of such name has value of a different type.</exception>
    public ClassRecord?[]? GetArrayOfClassRecords(string memberName, bool allowNulls = true, int maxLength = MaxLength)
        => GetMember<ArrayRecord<ClassRecord>>(memberName)?.ToArray(allowNulls, maxLength);
    /// <inheritdoc cref="GetArrayOfClassRecords(string)"/>
    public string?[]? GetArrayOfStrings(string memberName, bool allowNulls = true, int maxLength = MaxLength)
        => GetMember<ArrayRecord<string>>(memberName)?.ToArray(allowNulls, maxLength);
    /// <inheritdoc cref="GetArrayOfClassRecords(string)"/>
    public bool[]? GetArrayOfBooleans(string memberName, int maxLength = MaxLength)
        => GetMember<ArrayRecord<bool>>(memberName)?.ToArray(false, maxLength);
    /// <inheritdoc cref="GetArrayOfClassRecords(string)"/>
    public byte[]? GetArrayOfBytes(string memberName, int maxLength = MaxLength)
        => GetMember<ArrayRecord<byte>>(memberName)?.ToArray(false, maxLength);
    /// <inheritdoc cref="GetArrayOfClassRecords(string)"/>
    public sbyte[]? GetArrayOfSBytes(string memberName, int maxLength = MaxLength)
        => GetMember<ArrayRecord<sbyte>>(memberName)?.ToArray(false, maxLength);
    /// <inheritdoc cref="GetArrayOfClassRecords(string)"/>
    public short[]? GetArrayOfInt16s(string memberName, int maxLength = MaxLength)
        => GetMember<ArrayRecord<short>>(memberName)?.ToArray(false, maxLength);
    /// <inheritdoc cref="GetArrayOfClassRecords(string)"/>
    public ushort[]? GetArrayOfUInt16s(string memberName, int maxLength = MaxLength)
        => GetMember<ArrayRecord<ushort>>(memberName)?.ToArray(false, maxLength);
    /// <inheritdoc cref="GetArrayOfClassRecords(string)"/>
    public char[]? GetArrayOfChars(string memberName, int maxLength = MaxLength)
        => GetMember<ArrayRecord<char>>(memberName)?.ToArray(false, maxLength);
    /// <inheritdoc cref="GetArrayOfClassRecords(string)"/>
    public int[]? GetArrayOfInt32s(string memberName, int maxLength = MaxLength)
        => GetMember<ArrayRecord<int>>(memberName)?.ToArray(false, maxLength);
    /// <inheritdoc cref="GetArrayOfClassRecords(string)"/>
    public uint[]? GetArrayOfUInt32s(string memberName, int maxLength = MaxLength)
        => GetMember<ArrayRecord<uint>>(memberName)?.ToArray(false, maxLength);
    /// <inheritdoc cref="GetArrayOfClassRecords(string)"/>
    public float[]? GetArrayOfSingles(string memberName, int maxLength = MaxLength)
        => GetMember<ArrayRecord<float>>(memberName)?.ToArray(false, maxLength);
    /// <inheritdoc cref="GetArrayOfClassRecords(string)"/>
    public long[]? GetArrayOfInt64s(string memberName, int maxLength = MaxLength)
        => GetMember<ArrayRecord<long>>(memberName)?.ToArray(false, maxLength);
    /// <inheritdoc cref="GetArrayOfClassRecords(string)"/>
    public ulong[]? GetArrayOfUInt64s(string memberName, int maxLength = MaxLength)
        => GetMember<ArrayRecord<ulong>>(memberName)?.ToArray(false, maxLength);
    /// <inheritdoc cref="GetArrayOfClassRecords(string)"/>
    public double[]? GetArrayOfDoubles(string memberName, int maxLength = MaxLength)
        => GetMember<ArrayRecord<double>>(memberName)?.ToArray(false, maxLength);
    /// <inheritdoc cref="GetArrayOfClassRecords(string)"/>
    public decimal[]? GetArrayOfDecimals(string memberName, int maxLength = MaxLength)
        => GetMember<ArrayRecord<decimal>>(memberName)?.ToArray(false, maxLength);
    /// <inheritdoc cref="GetArrayOfClassRecords(string)"/>
    public TimeSpan[]? GetArrayOfTimeSpans(string memberName, int maxLength = MaxLength)
        => GetMember<ArrayRecord<TimeSpan>>(memberName)?.ToArray(false, maxLength);
    /// <inheritdoc cref="GetArrayOfClassRecords(string)"/>
    public DateTime[]? GetArrayOfDateTimes(string memberName, int maxLength = MaxLength)
        => GetMember<ArrayRecord<DateTime>>(memberName)?.ToArray(false, maxLength);
    /// <inheritdoc cref="GetArrayOfClassRecords(string)"/>
    public object?[]? GetArrayOfObjects(string memberName, bool allowNulls = true, int maxLength = MaxLength)
        => GetMember<ArrayRecord<object>>(memberName)?.ToArray(allowNulls, maxLength);

    public Array? GetJaggedArray(string memberName, Type expectedArrayType, bool allowNulls = true, int maxLength = MaxLength)
        => GetMember<BinaryArrayRecord>(memberName)?.ToArray(expectedArrayType, allowNulls, maxLength);

    public Array? GetRectangularArray(string memberName, Type expectedArrayType, bool allowNulls = true, int maxLength = MaxLength)
        => GetMember<RectangularOrCustomOffsetArrayRecord>(memberName)?.ToArray(expectedArrayType, allowNulls, maxLength);

    /// <summary>
    /// Retrieves the <see cref="SerializationRecord" /> of the provided <paramref name="memberName"/>.
    /// </summary>
    /// <param name="memberName">The name of the field.</param>
    /// <returns>The serialization record or null.</returns>
    /// <exception cref="KeyNotFoundException">Member of such name does not exist.</exception>
    /// <exception cref="InvalidOperationException">Member of such name has value of a different type or was a primitive value.</exception>
    public SerializationRecord? GetSerializationRecord(string memberName)
        => MemberValues[ClassInfo.MemberNames[memberName]] switch
        {
            null => null,
            MemberReferenceRecord referenceRecord => referenceRecord.GetReferencedRecord(),
            SerializationRecord serializationRecord => serializationRecord,
            _ => throw new InvalidOperationException()
        };

    private T? GetMember<T>(string memberName)
    {
        int index = ClassInfo.MemberNames[memberName];

        object? value = MemberValues[index];
        if (value is SerializationRecord record)
        {
            value = record.GetValue();
        }

        return value is null
            ? default
            : value is not T ? throw new InvalidOperationException() : (T)value!;
    }

    internal abstract (AllowedRecordTypes allowed, PrimitiveType primitiveType) GetNextAllowedRecordType();

    internal override void HandleNextRecord(SerializationRecord nextRecord, NextInfo info)
    {
        Debug.Assert(!(nextRecord is NullsRecord nullsRecord && nullsRecord.NullCount > 1));

        HandleNextValue(nextRecord, info);
    }

    internal override void HandleNextValue(object value, NextInfo info)
    {
        MemberValues.Add(value);

        if (MemberValues.Count < ExpectedValuesCount)
        {
            (AllowedRecordTypes allowed, PrimitiveType primitiveType) = GetNextAllowedRecordType();

            info.Stack.Push(info.With(allowed, primitiveType));
        }
    }
}
