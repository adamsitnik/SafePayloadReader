using System.Collections.Generic;
using System.IO;

namespace System.Runtime.Serialization.BinaryFormat;

/// <summary>
///  Class information that references another class record's metadata.
/// </summary>
/// <remarks>
///  <para>
///   <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/2d168388-37f4-408a-b5e0-e48dbce73e26">
///    [MS-NRBF] 2.3.2.5
///   </see>
///  </para>
/// </remarks>
internal sealed class ClassWithIdRecord : ClassRecord
{
    private ClassWithIdRecord(int objectId, ClassRecord metadataClass, IReadOnlyList<object> memberValues)
        : base(metadataClass.ClassInfo, memberValues)
    {
        ObjectId = objectId;
    }

    public override RecordType RecordType => RecordType.ClassWithId;

    internal override int ObjectId { get; }

    internal static ClassWithIdRecord Parse(
        BinaryReader reader,
        RecordMap recordMap)
    {
        int objectId = reader.ReadInt32();
        int metadataId = reader.ReadInt32();

        if (recordMap[metadataId] is not ClassRecord referencedRecord)
        {
            throw new SerializationException();
        }

        IReadOnlyList<object> memberValues = referencedRecord switch
        {
            ClassWithMembersAndTypesRecord classWithMembersAndTypes
                => classWithMembersAndTypes.MemberTypeInfo.ReadValues(reader, recordMap),
            SystemClassWithMembersAndTypesRecord systemClassWithMembersAndTypes
                => systemClassWithMembersAndTypes.MemberTypeInfo.ReadValues(reader, recordMap),
            ClassWithMembersRecord classWithMembers
                => ReadRecords(reader, recordMap, classWithMembers.MemberValues.Count),
            SystemClassWithMembersRecord systemClassWithMembers
                => ReadRecords(reader, recordMap, systemClassWithMembers.MemberValues.Count),
            _ => throw new SerializationException(),
        };

        return new(objectId, referencedRecord, memberValues);
    }
}