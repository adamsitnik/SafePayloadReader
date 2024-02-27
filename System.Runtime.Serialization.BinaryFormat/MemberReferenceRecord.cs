using System.IO;

namespace System.Runtime.Serialization.BinaryFormat;

/// <summary>
///  The <see cref="MemberReferenceRecord"/> record contains a reference to another record that contains the actual value.
/// </summary>
/// <remarks>
///  <para>
///   <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/eef0aa32-ab03-4b6a-a506-bcdfc10583fd">
///    [MS-NRBF] 2.5.3
///   </see>
///  </para>
/// </remarks>
internal sealed class MemberReferenceRecord : SerializationRecord
{
    public int Reference;

    private MemberReferenceRecord(int idRef) => Reference = idRef;

    public override RecordType RecordType => RecordType.MemberReference;

    internal static MemberReferenceRecord Parse(BinaryReader reader) => new(reader.ReadInt32());
}