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
internal sealed class ClassWithMembersAndTypesRecord : ClassRecord
{
    private ClassWithMembersAndTypesRecord(ClassInfo classInfo, BinaryLibraryRecord library,
        MemberTypeInfo memberTypeInfo, object[] memberValues)
        : base(classInfo, memberValues)
    {
        Library = library;
        MemberTypeInfo = memberTypeInfo;
    }

    public override RecordType RecordType => RecordType.ClassWithMembersAndTypes;

    internal BinaryLibraryRecord Library { get; }

    internal MemberTypeInfo MemberTypeInfo { get; }

    public override bool IsSerializedInstanceOf(Type type)
        => FormatterServices.GetTypeFullNameIncludingTypeForwards(type) == ClassInfo.Name
        && FormatterServices.GetAssemblyNameIncludingTypeForwards(type) == Library.LibraryName;

    internal static ClassWithMembersAndTypesRecord Parse(BinaryReader reader, Dictionary<int, SerializationRecord> recordMap)
    {
        ClassInfo classInfo = ClassInfo.Parse(reader);
        MemberTypeInfo memberTypeInfo = MemberTypeInfo.Parse(reader, classInfo.MemberNames.Length);
        int libraryId = reader.ReadInt32();

        // TODO: remove unbounded recursion
        object[] values = memberTypeInfo.ReadValues(reader, recordMap);

        BinaryLibraryRecord library = (BinaryLibraryRecord)recordMap[libraryId];

        return new(classInfo, library, memberTypeInfo, values);
    }
}