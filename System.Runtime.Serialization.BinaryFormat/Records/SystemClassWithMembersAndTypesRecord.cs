using System.Diagnostics;
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

    internal static bool CanBeMappedToPrimitive<T>()
        => typeof(T) == typeof(DateTime)
        || typeof(T) == typeof(TimeSpan)
        || typeof(T) == typeof(decimal)
        || typeof(T) == typeof(IntPtr)
        || typeof(T) == typeof(UIntPtr);

    internal T GetValue<T>()
    {
        Debug.Assert(CanBeMappedToPrimitive<T>());

        if (typeof(T) == typeof(DateTime))
        {
            long raw = (long)MemberValues[0]!;
            return (T)(object)BinaryReaderExtensions.CreateDateTimeFromData(raw);
        }
        else if (typeof(T) == typeof(TimeSpan))
        {
            long raw = (long)MemberValues[0]!;
            return (T)(object)new TimeSpan(raw);
        }
        else if (typeof(T) == typeof(decimal))
        {
            int[] bits =
            [
                (int)this["lo"]!,
                (int)this["mid"]!,
                (int)this["hi"]!,
                (int)this["flags"]!
            ];

            return (T)(object)new decimal(bits);
        }
        else if (typeof(T) == typeof(IntPtr))
        {
            long raw = (long)MemberValues[0]!;
            return (T)(object)new IntPtr(raw);
        }
        else
        {
            Debug.Assert(typeof(T) == typeof(UIntPtr));

            ulong raw = (ulong)MemberValues[0]!;
            return (T)(object)new UIntPtr(raw);
        }
    }
}
