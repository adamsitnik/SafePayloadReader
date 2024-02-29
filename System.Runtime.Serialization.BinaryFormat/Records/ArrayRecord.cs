namespace System.Runtime.Serialization.BinaryFormat;

public abstract class ArrayRecord<T> : SerializationRecord
{
    private protected ArrayRecord(T[] values) => Values = values;

    /// <summary>
    ///  Returns the item at the given index.
    /// </summary>
    public T this[int index] => Values[index];

    /// <summary>
    ///  Length of the array.
    /// </summary>
    public int Length => Values.Length;

    internal T[] Values { get; }
}
