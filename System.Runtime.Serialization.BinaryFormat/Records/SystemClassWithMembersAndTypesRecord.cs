using System.Collections.Generic;
using System.IO;

namespace System.Runtime.Serialization.BinaryFormat;

internal sealed class SystemClassWithMembersAndTypesRecord : ClassRecord
{
    private SystemClassWithMembersAndTypesRecord(ClassInfo classInfo, MemberTypeInfo memberTypeInfo, IReadOnlyList<object> values)
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
        BinaryReader reader, RecordMap recordMap)
    {
        ClassInfo classInfo = ClassInfo.Parse(reader);
        MemberTypeInfo memberTypeInfo = MemberTypeInfo.Parse(reader, classInfo.MemberNames.Count);
        // the only difference with ClassWithMembersAndTypesRecord is that we don't read library id here

        // TODO: remove unbounded recursion
        IReadOnlyList<object> values = memberTypeInfo.ReadValues(reader, recordMap);

        return new(classInfo, memberTypeInfo, values);
    }
}
