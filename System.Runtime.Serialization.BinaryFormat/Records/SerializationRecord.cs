﻿using System.Diagnostics;

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

    /// <summary>
    /// Compares the type and assembly name read from the payload against the specified type.
    /// </summary>
    /// <remarks>
    /// <para>It takes type forwarding into account.</para>
    /// <para>It does NOT take into account member names and their types.</para>
    /// </remarks>
    /// <param name="type">The <seealso cref="Type"/> to compare against.</param>
    /// <returns>True if the serialized type and assembly name match provided type.</returns>
    public virtual bool IsTypeNameMatching(Type type) => false;

    /// <summary>
    /// Gets the primitive, string or null record value.
    /// For reference records, it returns the referenced record.
    /// For other records, it returns the records themselves.
    /// </summary>
    internal virtual object? GetValue() => this;

    internal virtual void HandleNextRecord(SerializationRecord nextRecord, NextInfo info)
        => throw new InvalidOperationException("This should never happen");

    internal virtual void HandleNextValue(object value, NextInfo info)
        => throw new InvalidOperationException("This should never happen");
}