using System.IO;

namespace System.Runtime.Serialization.BinaryFormat;

/// <summary>
///  Library full name information.
/// </summary>
/// <remarks>
///  <para>
///   <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/7fcf30e1-4ad4-4410-8f1a-901a4a1ea832">
///    [MS-NRBF] 2.6.2
///   </see>
///  </para>
/// </remarks>
internal sealed class BinaryLibraryRecord : SerializationRecord
{
    private BinaryLibraryRecord(int libraryId, string libraryName)
    {
        Id = libraryId;
        LibraryName = libraryName;
    }

    public override RecordType RecordType => RecordType.BinaryLibrary;

    internal string LibraryName { get; }

    internal override int Id { get; }

    internal static BinaryLibraryRecord Parse(BinaryReader reader)
        => new(reader.ReadInt32(), reader.ReadString());
}