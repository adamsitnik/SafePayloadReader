using System.IO;

namespace System.Runtime.Serialization.BinaryFormat;

/// <summary>
///  Array information structure.
/// </summary>
/// <remarks>
///  <para>
///   <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/8fac763f-e46d-43a1-b360-80eb83d2c5fb">
///    [MS-NRBF] 2.4.2.1
///   </see>
///  </para>
/// </remarks>
internal readonly struct ArrayInfo
{
    internal ArrayInfo(int objectId, int length)
    {
        Length = length;
        ObjectId = objectId;
    }

    internal int ObjectId { get; }

    internal int Length { get; }

    internal static ArrayInfo Parse(BinaryReader reader)
    {
        int id = reader.ReadInt32();
        int length = reader.ReadInt32();

        if (length < 0 || length > 2147483591) // Array.MaxLength
        {
            throw new SerializationException($"Invalid array length: {length}");
        }

        return new(id, length);
    }
}
