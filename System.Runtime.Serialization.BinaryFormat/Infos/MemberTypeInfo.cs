﻿using System.Collections.Generic;
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

    internal (AllowedRecordTypes allowed, PrimitiveType primitiveType) GetNextAllowedRecordType(int currentValuesCount)
    {
        (BinaryType binaryType, object? additionalInfo) = Infos[currentValuesCount];

        // Every array can be either an array itself, a null or a reference (to an array)
        const AllowedRecordTypes stringArray = AllowedRecordTypes.ArraySingleString
            | AllowedRecordTypes.ObjectNull | AllowedRecordTypes.MemberReference;
        const AllowedRecordTypes primitiveArray = AllowedRecordTypes.ArraySinglePrimitive 
            | AllowedRecordTypes.ObjectNull | AllowedRecordTypes.MemberReference;
        const AllowedRecordTypes objectArray = AllowedRecordTypes.ArraySingleObject
            | AllowedRecordTypes.ObjectNull | AllowedRecordTypes.MemberReference;

        // Every string can be a string, a null or a reference (to a string)
        const AllowedRecordTypes strings = AllowedRecordTypes.BinaryObjectString
            | AllowedRecordTypes.ObjectNull | AllowedRecordTypes.MemberReference;

        // Every class can be a null or a reference and a ClassWithId (TODO: verify ClassId)
        const AllowedRecordTypes classes = AllowedRecordTypes.ClassWithId
            | AllowedRecordTypes.ObjectNull | AllowedRecordTypes.MemberReference;
        // but System classes can be expressed only by System records
        const AllowedRecordTypes systemClass = classes
            | AllowedRecordTypes.SystemClassWithMembers | AllowedRecordTypes.SystemClassWithMembersAndTypes;
        const AllowedRecordTypes nonSystemClass = classes
            | AllowedRecordTypes.ClassWithMembers | AllowedRecordTypes.ClassWithMembersAndTypes;

        return binaryType switch
        {
            BinaryType.Primitive => (default, (PrimitiveType)additionalInfo!),
            BinaryType.String => (strings, default),
            BinaryType.Object => (AllowedRecordTypes.AnyObject, default),
            BinaryType.StringArray => (stringArray, default),
            BinaryType.PrimitiveArray => (primitiveArray, default),
            BinaryType.Class => (nonSystemClass, default),
            BinaryType.SystemClass => (systemClass, default),
            BinaryType.ObjectArray => (objectArray, default),
            _ => throw new SerializationException($"Invalid binary type: {binaryType}.")
        };
    }

    internal bool IsElementType(Type typeElement, RecordMap recordMap)
    {
        (BinaryType binaryType, object? additionalInfo) = Infos[0];

        switch (binaryType)
        {
            case BinaryType.String:
                return typeElement == typeof(string);
            case BinaryType.StringArray:
                return typeElement == typeof(string[]);
            case BinaryType.Object:
                return typeElement == typeof(object);
            case BinaryType.ObjectArray:
                return typeElement == typeof(object[]);
            case BinaryType.Primitive:
            case BinaryType.PrimitiveArray:
                if (binaryType is BinaryType.PrimitiveArray)
                {
                    if (!typeElement.IsArray)
                    {
                        return false;
                    }
                    typeElement = typeElement.GetElementType()!;
                }
                return ((PrimitiveType)additionalInfo!) switch
                {
                    PrimitiveType.Boolean => typeElement == typeof(bool),
                    PrimitiveType.Byte => typeElement == typeof(byte),
                    PrimitiveType.Char => typeElement == typeof(char),
                    PrimitiveType.Decimal => typeElement == typeof(decimal),
                    PrimitiveType.Double => typeElement == typeof(double),
                    PrimitiveType.Int16 => typeElement == typeof(short),
                    PrimitiveType.Int32 => typeElement == typeof(int),
                    PrimitiveType.Int64 => typeElement == typeof(long),
                    PrimitiveType.SByte => typeElement == typeof(sbyte),
                    PrimitiveType.Single => typeElement == typeof(float),
                    PrimitiveType.TimeSpan => typeElement == typeof(TimeSpan),
                    PrimitiveType.DateTime => typeElement == typeof(DateTime),
                    PrimitiveType.UInt16 => typeElement == typeof(ushort),
                    PrimitiveType.UInt32 => typeElement == typeof(uint),
                    PrimitiveType.UInt64 => typeElement == typeof(ulong),
                    _ => false
                };
            case BinaryType.SystemClass:
                if (typeElement.Assembly != typeof(object).Assembly)
                {
                    return false;
                }
                string fullTypeName = (string)additionalInfo!;
                string fullSystemClassName = FormatterServices.GetTypeFullNameIncludingTypeForwards(typeElement);
                return fullTypeName == fullSystemClassName;
            case BinaryType.Class:
                ClassTypeInfo typeInfo = (ClassTypeInfo)additionalInfo!;
                string fullClassName = FormatterServices.GetTypeFullNameIncludingTypeForwards(typeElement);
                if (typeInfo.TypeName != fullClassName)
                {
                    return false;
                }
                BinaryLibraryRecord libraryRecord = (BinaryLibraryRecord)recordMap[typeInfo.LibraryId];
                string assemblyName = FormatterServices.GetAssemblyNameIncludingTypeForwards(typeElement);
                return assemblyName == libraryRecord.LibraryName;
            default:
                throw new NotSupportedException();
        }
    }

    internal bool ShouldBeRepresentedAsArrayOfClassRecords()
    {
        (BinaryType binaryType, object? additionalInfo) = Infos[0];

        switch (binaryType)
        {
            case BinaryType.SystemClass:
                string fullTypeName = (string)additionalInfo!;
                // TODO: use TypeName here!
                return !fullTypeName.EndsWith("[]", StringComparison.Ordinal);
            case BinaryType.Class:
                ClassTypeInfo typeInfo = (ClassTypeInfo)additionalInfo!;
                // TODO: use TypeName here!
                return !typeInfo.TypeName.EndsWith("[]", StringComparison.Ordinal);
            default:
                return false;
        }
    }
}
