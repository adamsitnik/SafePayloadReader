using System.Collections.Generic;
using System.IO;

namespace System.Runtime.Serialization.BinaryFormat;

/// <summary>
///  Class information with type info and the source library.
/// </summary>
/// <remarks>
///  <para>
///   <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/847b0b6a-86af-4203-8ed0-f84345f845b9">
///    [MS-NRBF] 2.3.2.1
///   </see>
///  </para>
/// </remarks>
public sealed class ClassWithMembersAndTypesRecord : SerializationRecord
{
    internal ClassInfo ClassInfo { get; }
    internal MemberTypeInfo MemberTypeInfo { get; }
    internal object[] MemberValues { get; }
    public int LibraryId { get; }

    private ClassWithMembersAndTypesRecord(ClassInfo classInfo, int libraryId, MemberTypeInfo memberTypeInfo, object[] memberValues)
    {
        ClassInfo = classInfo;
        MemberTypeInfo = memberTypeInfo;
        LibraryId = libraryId;
        MemberValues = memberValues;
    }

    public override RecordType RecordType => RecordType.ClassWithMembersAndTypes;

    internal override int Id => ClassInfo.ObjectId;

    internal override bool IsFollowedByInlineData => true;

    internal static ClassWithMembersAndTypesRecord Parse(BinaryReader reader, Dictionary<int, SerializationRecord> recordMap)
    {
        ClassInfo classInfo = ClassInfo.Parse(reader);
        MemberTypeInfo memberTypeInfo = MemberTypeInfo.Parse(reader, classInfo.MemberNames.Length);

        ClassWithMembersAndTypesRecord record = new(
            classInfo,
            reader.ReadInt32(),
            memberTypeInfo,
            memberTypeInfo.ReadValuesFromMemberTypeInfo(reader, recordMap));

        return record;
    }
}