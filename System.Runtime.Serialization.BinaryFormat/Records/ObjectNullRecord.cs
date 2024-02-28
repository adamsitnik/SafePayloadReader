namespace System.Runtime.Serialization.BinaryFormat;

/// <summary>
///  Null object record.
/// </summary>
/// <remarks>
///  <para>
///   <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/fe51522c-23d1-48dd-9913-c84894abc127">
///    [MS-NRBF] 2.5.4
///   </see>
///  </para>
/// </remarks>
internal sealed class ObjectNullRecord : SerializationRecord
{
    internal static ObjectNullRecord Instance { get; } = new();

    public override RecordType RecordType => RecordType.ObjectNull;

    public override object? GetValue() => null;
}