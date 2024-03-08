using System.IO;

namespace System.Runtime.Serialization.BinaryFormat;

internal sealed class SystemClassWithMembersAndTypesRecord : ClassRecord
{
    private SystemClassWithMembersAndTypesRecord(ClassInfo classInfo, MemberTypeInfo memberTypeInfo)
        : base(classInfo)
    {
        MemberTypeInfo = memberTypeInfo;
    }

    public override RecordType RecordType => RecordType.SystemClassWithMembersAndTypes;

    public MemberTypeInfo MemberTypeInfo { get; }

    internal override int ExpectedValuesCount => MemberTypeInfo.Infos.Count;

    public override bool IsSerializedInstanceOf(Type type)
        => type.Assembly == typeof(object).Assembly
        && FormatterServices.GetTypeFullNameIncludingTypeForwards(type) == ClassInfo.Name;

    internal static SystemClassWithMembersAndTypesRecord Parse(BinaryReader reader)
    {
        ClassInfo classInfo = ClassInfo.Parse(reader);
        MemberTypeInfo memberTypeInfo = MemberTypeInfo.Parse(reader, classInfo.MemberNames.Count);
        // the only difference with ClassWithMembersAndTypesRecord is that we don't read library id here
        return new(classInfo, memberTypeInfo);
    }

    internal override (AllowedRecordTypes allowed, PrimitiveType primitiveType) GetNextAllowedRecordType()
        => MemberTypeInfo.GetNextAllowedRecordType(MemberValues.Count);
}
