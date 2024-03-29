﻿namespace System.Runtime.Serialization.BinaryFormat;

[Flags]
internal enum AllowedRecordTypes : uint
{
    None = 0,
    SerializedStreamHeader = 1 << RecordType.SerializedStreamHeader,
    ClassWithId = 1 << RecordType.ClassWithId,
    SystemClassWithMembers = 1 << RecordType.SystemClassWithMembers,
    ClassWithMembers = 1 << RecordType.ClassWithMembers,
    SystemClassWithMembersAndTypes = 1 << RecordType.SystemClassWithMembersAndTypes,
    ClassWithMembersAndTypes = 1 << RecordType.ClassWithMembersAndTypes,
    BinaryObjectString = 1 << RecordType.BinaryObjectString,
    BinaryArray = 1 << RecordType.BinaryArray,
    MemberPrimitiveTyped = 1 << RecordType.MemberPrimitiveTyped,
    MemberReference = 1 << RecordType.MemberReference,
    ObjectNull = 1 << RecordType.ObjectNull,
    MessageEnd = 1 << RecordType.MessageEnd,
    BinaryLibrary = 1 << RecordType.BinaryLibrary,
    ObjectNullMultiple256 = 1 << RecordType.ObjectNullMultiple256,
    ObjectNullMultiple = 1 << RecordType.ObjectNullMultiple,
    ArraySinglePrimitive = 1 << RecordType.ArraySinglePrimitive,
    ArraySingleObject = 1 << RecordType.ArraySingleObject,
    ArraySingleString = 1 << RecordType.ArraySingleString,
    CrossAppDomainMap = 1 << RecordType.CrossAppDomainMap,
    CrossAppDomainString = 1 << RecordType.CrossAppDomainString,
    CrossAppDomainAssembly = 1 << RecordType.CrossAppDomainAssembly,
    MethodCall = 1 << RecordType.MethodCall,
    MethodReturn = 1 << RecordType.MethodReturn,

    Nulls = ObjectNull | ObjectNullMultiple256 | ObjectNullMultiple,

    /// <summary>
    /// Any .NET object (a primitive, a reference type, a reference or single null).
    /// </summary>
    AnyObject = MemberPrimitiveTyped
        | ArraySingleObject | ArraySinglePrimitive | ArraySingleString | BinaryArray
        | ClassWithId | ClassWithMembers | ClassWithMembersAndTypes | SystemClassWithMembers | SystemClassWithMembersAndTypes
        | BinaryObjectString
        | MemberReference
        | ObjectNull,
}