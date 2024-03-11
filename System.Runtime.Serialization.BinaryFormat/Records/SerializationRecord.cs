using System.Diagnostics;

namespace System.Runtime.Serialization.BinaryFormat;

[DebuggerDisplay("{RecordType}, {ObjectId}")]
public abstract class SerializationRecord
{
    internal const int NoId = 0;

    internal SerializationRecord() // others can't derive from this type
    {
    }

    public abstract RecordType RecordType { get; }

    internal virtual int ObjectId => NoId;

    // TODO: find a better name
    public virtual bool IsSerializedInstanceOf(Type type) => false;

    internal virtual object? GetValue() => this;

    internal virtual void HandleNextRecord(SerializationRecord nextRecord, NextInfo info)
        => throw new InvalidOperationException("This should never happen");

    internal virtual void HandleNextValue(object value, NextInfo info)
        => throw new InvalidOperationException("This should never happen");
}