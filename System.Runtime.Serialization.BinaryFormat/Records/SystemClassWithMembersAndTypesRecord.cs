using System.Collections.Generic;
using System.IO;

namespace System.Runtime.Serialization.BinaryFormat;

internal sealed class SystemClassWithMembersAndTypesRecord : ClassRecord
{
    private SystemClassWithMembersAndTypesRecord(ClassInfo classInfo, MemberTypeInfo memberTypeInfo, object[] values)
        : base(classInfo, values)
    {
        MemberTypeInfo = memberTypeInfo;
    }

    public override RecordType RecordType => RecordType.SystemClassWithMembersAndTypes;

    public MemberTypeInfo MemberTypeInfo { get; }

    public override bool IsSerializedInstanceOf(Type type)
        => type.Assembly == typeof(object).Assembly
        && FormatterServices.GetTypeFullNameIncludingTypeForwards(type) == ClassInfo.Name;

    internal static SystemClassWithMembersAndTypesRecord Parse(
        BinaryReader reader, Dictionary<int, SerializationRecord> recordMap)
    {
        ClassInfo classInfo = ClassInfo.Parse(reader);
        MemberTypeInfo memberTypeInfo = MemberTypeInfo.Parse(reader, classInfo.MemberNames.Length);
        // the only difference with ClassWithMembersAndTypesRecord is that we don't read library id here

        // TODO: remove unbounded recursion
        object[] values = memberTypeInfo.ReadValues(reader, recordMap);

        return new(classInfo, memberTypeInfo, values);
    }
}
