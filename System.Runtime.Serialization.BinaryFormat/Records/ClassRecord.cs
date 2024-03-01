using System.Collections.Generic;

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
    private protected ClassRecord(ClassInfo classInfo, object[] memberValues)
    {
        ClassInfo = classInfo;
        MemberValues = memberValues;
    }

    public string TypeName => ClassInfo.Name;

    public virtual IReadOnlyList<string> MemberNames => ClassInfo.MemberNames;

    internal override int ObjectId => ClassInfo.ObjectId;

    internal virtual ClassInfo ClassInfo { get; }

    internal virtual object[] MemberValues { get; }

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
    public virtual object? this[string memberName]
    {
        get
        {
            int index = Array.IndexOf(ClassInfo.MemberNames, memberName);
            if (index < 0)
            {
                throw new KeyNotFoundException();
            }

            object value = MemberValues[index];
            if (value is SerializationRecord record)
            {
                return record.GetValue();
            }
            return value;
        }
    }
}
