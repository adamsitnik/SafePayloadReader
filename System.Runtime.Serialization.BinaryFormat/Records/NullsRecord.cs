namespace System.Runtime.Serialization.BinaryFormat;

internal abstract class NullsRecord : SerializationRecord
{
    internal abstract int NullCount { get; }
}