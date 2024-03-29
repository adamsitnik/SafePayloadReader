﻿using System.IO;

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
    private ClassWithMembersAndTypesRecord(ClassInfo classInfo, BinaryLibraryRecord library, MemberTypeInfo memberTypeInfo)
        : base(classInfo)
    {
        Library = library;
        MemberTypeInfo = memberTypeInfo;
    }

    public override RecordType RecordType => RecordType.ClassWithMembersAndTypes;

    public override string LibraryName => Library.LibraryName;

    internal BinaryLibraryRecord Library { get; }

    internal MemberTypeInfo MemberTypeInfo { get; }

    internal override int ExpectedValuesCount => MemberTypeInfo.Infos.Count;

    public override bool IsTypeNameMatching(Type type)
        => FormatterServices.GetTypeFullNameIncludingTypeForwards(type) == ClassInfo.Name
        && FormatterServices.GetAssemblyNameIncludingTypeForwards(type) == Library.LibraryName;

    internal static ClassWithMembersAndTypesRecord Parse(BinaryReader reader, RecordMap recordMap)
    {
        ClassInfo classInfo = ClassInfo.Parse(reader);
        MemberTypeInfo memberTypeInfo = MemberTypeInfo.Parse(reader, classInfo.MemberNames.Count);
        int libraryId = reader.ReadInt32();

        BinaryLibraryRecord library = (BinaryLibraryRecord)recordMap[libraryId];

        return new(classInfo, library, memberTypeInfo);
    }

    internal override (AllowedRecordTypes allowed, PrimitiveType primitiveType) GetNextAllowedRecordType()
        => MemberTypeInfo.GetNextAllowedRecordType(MemberValues.Count);
}