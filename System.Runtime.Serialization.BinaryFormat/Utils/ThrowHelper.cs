namespace System.Runtime.Serialization.BinaryFormat;

internal static class ThrowHelper
{
    internal static void ThrowUnexpectedNullRecordCount()
        => throw new SerializationException("Unexpected Null Record count.");

    internal static Exception InvalidPrimitiveType(PrimitiveType primitiveType)
        => new SerializationException($"Invalid primitive type: {primitiveType}");

    internal static Exception InvalidBinaryType(BinaryType binaryType)
        => new SerializationException($"Invalid binary type: {binaryType}");

    internal static void ThrowMaxArrayLength(int limit, uint actual)
        => throw new SerializationException(
            $"The serialized array length ({actual}) was larger that the configured limit {limit}");
}
