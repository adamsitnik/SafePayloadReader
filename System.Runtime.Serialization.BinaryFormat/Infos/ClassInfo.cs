using System.IO;

namespace System.Runtime.Serialization.BinaryFormat;

/// <summary>
///  Class info.
/// </summary>
/// <remarks>
///  <para>
///   <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/0a192be0-58a1-41d0-8a54-9c91db0ab7bf">
///    [MS-NRBF] 2.3.1.1
///   </see>
///  </para>
/// </remarks>
internal sealed class ClassInfo
{
    internal ClassInfo(int objectId, string name, string[] memberNames)
    {
        ObjectId = objectId;
        Name = name;
        MemberNames = memberNames;
    }

    internal int ObjectId { get; }

    internal string Name { get; }

    internal string[] MemberNames { get; }

    internal static ClassInfo Parse(BinaryReader reader)
    {
        int objectId = reader.ReadInt32();
        string name = reader.ReadString();
        int memberCount = reader.ReadInt32();
        string[] memberNames = new string[memberCount];

        for (int i = 0; i < memberCount; i++)
        {
            memberNames[i] = reader.ReadString();
        }

        return new(objectId, name, memberNames);
    }
}