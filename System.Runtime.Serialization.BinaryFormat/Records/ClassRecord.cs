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
    private protected ClassRecord(ClassInfo classInfo)
    {
        ClassInfo = classInfo;
        MemberValues = new();
    }

    public string TypeName => ClassInfo.Name;

    // Currently we don't expose raw values, so we are not preserving the order here.
    public IEnumerable<string> MemberNames => ClassInfo.MemberNames.Keys;

    internal override int ObjectId => ClassInfo.ObjectId;

    internal abstract int ExpectedValuesCount { get; }

    internal ClassInfo ClassInfo { get; }

    internal List<object?> MemberValues { get; }

    /// <summary>
    /// Retrieves the value of provided field.
    /// </summary>
    /// <param name="memberName">The name of the field.</param>
    /// <returns>
    /// For primitive types like <seealso cref="int"/> and <seealso cref="string"/> returns their value,
    /// for arrays of such types returns the arrays,
    /// for other types returns <seealso cref="ClassRecord"/> or <seealso cref="ArrayRecord{T}"/>.
    /// </returns>
    /// <exception cref="KeyNotFoundException">Member of such name does not exist.</exception>
    public object? this[string memberName]
    {
        get
        {
            int index = ClassInfo.MemberNames[memberName];

            object? value = MemberValues[index];
            if (value is SerializationRecord record)
            {
                return record.GetValue();
            }
            return value;
        }
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
