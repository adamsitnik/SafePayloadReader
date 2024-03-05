using System.Collections.Generic;

namespace System.Runtime.Serialization.BinaryFormat;

public abstract class ArrayRecord<T> : SerializationRecord
{
    private protected ArrayRecord(IReadOnlyList<T> values) => Values = values;

    /// <summary>
    ///  Returns the item at the given index.
    /// </summary>
    public T this[int index] => Values[index];

    /// <summary>
    ///  Length of the array.
    /// </summary>
    public int Length => Values.Count;

    internal IReadOnlyList<T> Values { get; }
}
