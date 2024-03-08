namespace System.Runtime.Serialization.BinaryFormat;

internal static class ThrowHelper
{
    internal static void ThrowUnexpectedNullRecordCount()
        => throw new SerializationException("Unexpected Null Record count.");
}
