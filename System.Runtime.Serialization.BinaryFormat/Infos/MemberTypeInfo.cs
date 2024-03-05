using System.Collections.Generic;
using System.IO;

namespace System.Runtime.Serialization.BinaryFormat;

/// <summary>
///  Member type info.
/// </summary>
/// <remarks>
///  <para>
///   <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/aa509b5a-620a-4592-a5d8-7e9613e0a03e">
///    [MS-NRBF] 2.3.1.2
///   </see>
///  </para>
/// </remarks>
internal readonly struct MemberTypeInfo
{
    internal MemberTypeInfo(IReadOnlyList<(BinaryType BinaryType, object? AdditionalInfo)> infos) => Infos = infos;

    internal readonly IReadOnlyList<(BinaryType BinaryType, object? AdditionalInfo)> Infos;

    internal static MemberTypeInfo Parse(BinaryReader reader, int count)
    {
        List<(BinaryType BinaryType, object? AdditionalInfo)> info = new();

        // [MS-NRBF] 2.3.1.2
        // https://docs.microsoft.com/en-us/openspecs/windows_protocols/ms-nrbf/aa509b5a-620a-4592-a5d8-7e9613e0a03e

        // All of the BinaryTypeEnumeration values come before all of the AdditionalInfo values.
        // There's not necessarily a 1:1 mapping; some enum values don't have associated AdditionalInfo.
        for (int i = 0; i < count; i++)
        {
            info.Add(((BinaryType)reader.ReadByte(), null));
        }

        // Check for more clarifying information
        for (int i = 0; i < info.Count; i++)
        {
            BinaryType type = info[i].BinaryType;
            switch (type)
            {
                case BinaryType.Primitive:
                case BinaryType.PrimitiveArray:
                    info[i] = (type, (PrimitiveType)reader.ReadByte());
                    break;
                case BinaryType.SystemClass:
                    info[i] = (type, reader.ReadString());
                    break;
                case BinaryType.Class:
                    info[i] = (type, ClassTypeInfo.Parse(reader));
                    break;
                case BinaryType.String:
                case BinaryType.ObjectArray:
                case BinaryType.StringArray:
                case BinaryType.Object:
                    // Other types have no additional data.
                    break;
                default:
                    throw new SerializationException("Unexpected binary type.");
            }
        }

        return new MemberTypeInfo(info);
    }

    internal IReadOnlyList<object> ReadValues(BinaryReader reader, RecordMap recordMap)
    {
        List<object> memberValues = new();

        for (int i = 0; i < Infos.Count; i++)
        {
            memberValues.Add(SerializationRecord.ReadValue(reader, recordMap, Infos[i].BinaryType, Infos[i].AdditionalInfo));
        }

        return memberValues;
    }
}
