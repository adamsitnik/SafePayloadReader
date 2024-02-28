namespace System.Runtime.Serialization.BinaryFormat;

/// <summary>
///  Record type.
/// </summary>
/// <remarks>
///  <para>
///   <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/954a0657-b901-4813-9398-4ec732fe8b32">
///    [MS-NRBF] 2.1.2.1
///   </see>
///  </para>
/// </remarks>
public enum RecordType : byte
{
    SerializedStreamHeader,
    ClassWithId,
    SystemClassWithMembers,
    ClassWithMembers,
    SystemClassWithMembersAndTypes,
    ClassWithMembersAndTypes,
    BinaryObjectString,
    BinaryArray,
    MemberPrimitiveTyped,
    MemberReference,
    ObjectNull,
    MessageEnd,
    BinaryLibrary,
    ObjectNullMultiple256,
    ObjectNullMultiple,
    ArraySinglePrimitive,
    ArraySingleObject,
    ArraySingleString,

    /// <summary>
    ///  Used for remote method invocation. <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/4c727b2f-2c30-468d-b12e-b56406f14862">
    ///  [MS-NRBF] 2.2</see>
    /// </summary>
    MethodCall,

    /// <summary>
    ///  Used for remote method invocation. <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/4c727b2f-2c30-468d-b12e-b56406f14862">
    ///  [MS-NRBF] 2.2</see>
    /// </summary>
    MethodReturn
}
