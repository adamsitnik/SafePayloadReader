﻿using System.Diagnostics;
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

    public override string LibraryName => FormatterServices.CoreLibAssemblyName;

    public MemberTypeInfo MemberTypeInfo { get; }

    internal override int ExpectedValuesCount => MemberTypeInfo.Infos.Count;

    public override bool IsTypeNameMatching(Type type)
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

    // For the root records that turn out to be primitive types, we map them to
    // PrimitiveTypeRecord<T> so the users don't need to learn the BF internals
    // to get a single primitive value!
    internal SerializationRecord TryToMapToUserFriendly()
    {
        // it could be implemented with way fewer ifs, but perf is important
        if (MemberValues.Count == 1)
        {
            return MemberValues[0] switch
            {
                // there can be a value match, but no TypeName match
                bool value when IsTypeNameMatching(typeof(bool)) => Create(value),
                byte value when IsTypeNameMatching(typeof(byte)) => Create(value),
                sbyte value when IsTypeNameMatching(typeof(sbyte)) => Create(value),
                char value when IsTypeNameMatching(typeof(char)) => Create(value),
                short value when IsTypeNameMatching(typeof(short)) => Create(value),
                ushort value when IsTypeNameMatching(typeof(ushort)) => Create(value),
                int value when IsTypeNameMatching(typeof(int)) => Create(value),
                uint value when IsTypeNameMatching(typeof(uint)) => Create(value),
                long value when IsTypeNameMatching(typeof(long)) => Create(value),
                long value when IsTypeNameMatching(typeof(TimeSpan)) => Create(new TimeSpan(value)),
                ulong value when IsTypeNameMatching(typeof(ulong)) => Create(value),
                float value when IsTypeNameMatching(typeof(float)) => Create(value),
                double value when IsTypeNameMatching(typeof(double)) => Create(value),
                _ => this
            };
        }
        else if (MemberValues.Count == 2
            && HasMember("ticks") && HasMember("dateData")
            && MemberValues[0] is long value && MemberValues[1] is ulong
            && IsTypeNameMatching(typeof(DateTime)))
        {
            return Create(BinaryReaderExtensions.CreateDateTimeFromData(value));
        }
        else if(MemberValues.Count == 4
            && HasMember("lo") && HasMember("mid") && HasMember("hi") && HasMember("flags")
            && MemberValues[0] is int && MemberValues[1] is int && MemberValues[2] is int && MemberValues[3] is int
            && IsTypeNameMatching(typeof(decimal)))
        {
            int[] bits =
            [
                GetInt32("lo"),
                GetInt32("mid"),
                GetInt32("hi"),
                GetInt32("flags")
            ];

            return Create(new decimal(bits));
        }

        return this;

        static SerializationRecord Create<T>(T value) where T : unmanaged
            => new MemberPrimitiveTypedRecord<T>(value, pretend: true);
    }
}
