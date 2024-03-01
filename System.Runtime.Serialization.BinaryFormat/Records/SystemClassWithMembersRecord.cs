using System.Collections.Generic;
using System.IO;

namespace System.Runtime.Serialization.BinaryFormat;

/// <summary>
///  System class information.
/// </summary>
/// <remarks>
///  <para>
///   <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/f5bd730f-d944-42ab-b6b3-013099559a4b">
///    [MS-NRBF] 2.3.2.4
///   </see>
///  </para>
/// </remarks>
internal sealed class SystemClassWithMembersRecord : ClassRecord
{
    private SystemClassWithMembersRecord(ClassInfo classInfo, object[] memberValues)
        : base(classInfo, memberValues)
    {
    }

    public override RecordType RecordType => RecordType.SystemClassWithMembers;

    public override bool IsSerializedInstanceOf(Type type)
        => type.Assembly == typeof(object).Assembly
        && FormatterServices.GetTypeFullNameIncludingTypeForwards(type) == ClassInfo.Name;

    internal static SystemClassWithMembersRecord Parse(BinaryReader reader, Dictionary<int, SerializationRecord> recordMap)
    {
        ClassInfo classInfo = ClassInfo.Parse(reader);
        // the only difference with ClassWithMembersRecord is that we don't read library id here
        object[] values = ReadRecords(reader, recordMap, classInfo.MemberNames.Length);

        return new(classInfo, values);
    }
}
