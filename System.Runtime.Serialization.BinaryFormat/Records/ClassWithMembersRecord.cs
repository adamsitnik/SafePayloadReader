using System.Collections.Generic;
using System.IO;

namespace System.Runtime.Serialization.BinaryFormat;

/// <summary>
///  Class information with the source library.
/// </summary>
/// <remarks>
///  <para>
///   <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/ebbdad88-91fe-48ae-a985-661f9cc7e0de">
///    [MS-NRBF] 2.3.2.2
///   </see>
///  </para>
/// </remarks>
internal sealed class ClassWithMembersRecord : ClassRecord
{
    private ClassWithMembersRecord(ClassInfo classInfo, BinaryLibraryRecord library, object[] memberValues) 
        : base(classInfo, memberValues)
    {
        Library = library;
    }

    public override RecordType RecordType => RecordType.ClassWithMembers;

    internal BinaryLibraryRecord Library { get; }

    public override bool IsSerializedInstanceOf(Type type)
        => FormatterServices.GetTypeFullNameIncludingTypeForwards(type) == ClassInfo.Name
        && FormatterServices.GetAssemblyNameIncludingTypeForwards(type) == Library.LibraryName;

    internal static ClassWithMembersRecord Parse(BinaryReader reader, RecordMap recordMap)
    {
        ClassInfo classInfo = ClassInfo.Parse(reader);
        int libraryId = reader.ReadInt32();
        object[] memberValues = ReadRecords(reader, recordMap, classInfo.MemberNames.Length);

        BinaryLibraryRecord library = (BinaryLibraryRecord)recordMap[libraryId];

        return new(classInfo, library, memberValues);
    }
}
