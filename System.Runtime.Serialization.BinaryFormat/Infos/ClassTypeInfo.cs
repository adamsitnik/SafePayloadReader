using System.IO;

namespace System.Runtime.Serialization.BinaryFormat;

/// <summary>
///  Identifies a class by it's name and library id.
/// </summary>
/// <remarks>
///  <para>
///   <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/844b24dd-9f82-426e-9b98-05334307a239">
///    [MS-NRBF] 2.1.1.8
///   </see>
///  </para>
/// </remarks>
internal sealed class ClassTypeInfo
{
    internal ClassTypeInfo(string typeName, int libraryId)
    {
        TypeName = typeName;
        LibraryId = libraryId;
    }

    internal string TypeName { get; }
    internal int LibraryId { get; }

    internal static ClassTypeInfo Parse(BinaryReader reader) => new(
        reader.ReadString(),
        reader.ReadInt32());
}