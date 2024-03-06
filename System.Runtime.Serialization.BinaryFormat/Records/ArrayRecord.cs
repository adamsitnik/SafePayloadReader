namespace System.Runtime.Serialization.BinaryFormat;

public abstract class ArrayRecord<T> : SerializationRecord
{
    private protected ArrayRecord(ArrayInfo arrayInfo) => ArrayInfo = arrayInfo;

    /// <summary>
    /// Length of the array.
    /// </summary>
    public int Length => ArrayInfo.Length;

    internal override int ObjectId => ArrayInfo.ObjectId;

    private ArrayInfo ArrayInfo { get; }

    /// <summary>
    /// Allocates an array of <typeparamref name="T"/> and fills it with the data provided in the serialized records (in case of primitive types like <see cref="string"/> or <see cref="int"/>) or the serialized records themselves.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The array has <seealso cref="ArrayRecord.Length"/> elements and can be used as a vector of attack.
    /// Example: an array with <seealso cref="Array.MaxLength"/> elements that contains only nulls
    /// takes 15 bytes to serialize and more than 2 GB to deserialize!
    /// </para>
    /// <para>
    /// A new array is allocated every time this method is called.
    /// </para>
    /// </remarks>
    public abstract T?[] Deserialize(bool allowNulls = true);
}
