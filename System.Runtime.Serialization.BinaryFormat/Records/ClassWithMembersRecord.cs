﻿using System.IO;

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
    private ClassWithMembersRecord(ClassInfo classInfo, BinaryLibraryRecord library) : base(classInfo)
    {
        Library = library;
    }

    public override RecordType RecordType => RecordType.ClassWithMembers;

    public override string LibraryName => Library.LibraryName;

    internal BinaryLibraryRecord Library { get; }

    internal override int ExpectedValuesCount => ClassInfo.MemberNames.Count;

    public override bool IsTypeNameMatching(Type type)
        => FormatterServices.GetTypeFullNameIncludingTypeForwards(type) == ClassInfo.Name
        && FormatterServices.GetAssemblyNameIncludingTypeForwards(type) == Library.LibraryName;

    internal static ClassWithMembersRecord Parse(BinaryReader reader, RecordMap recordMap)
    {
        ClassInfo classInfo = ClassInfo.Parse(reader);
        int libraryId = reader.ReadInt32();
        
        BinaryLibraryRecord library = (BinaryLibraryRecord)recordMap[libraryId];

        return new(classInfo, library);
    }

    internal override (AllowedRecordTypes allowed, PrimitiveType primitiveType) GetNextAllowedRecordType()
        => (AllowedRecordTypes.AnyObject, PrimitiveType.None);
}
