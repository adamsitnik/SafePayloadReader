namespace System.Runtime.Serialization.BinaryFormat;

public abstract class ArrayRecord : SerializationRecord
{
    private protected ArrayRecord(ArrayInfo arrayInfo)
    {
        ArrayInfo = arrayInfo;
        ValuesToRead = arrayInfo.Length;
    }

    /// <summary>
    /// Length of the array.
    /// </summary>
    public uint Length => ArrayInfo.Length;

    internal override int ObjectId => ArrayInfo.ObjectId;

    private protected ArrayInfo ArrayInfo { get; }

    internal long ValuesToRead { get; private protected set; }

    internal abstract (AllowedRecordTypes allowed, PrimitiveType primitiveType) GetAllowedRecordType();
}

public abstract class ArrayRecord<T> : ArrayRecord
{
    private protected ArrayRecord(ArrayInfo arrayInfo) : base(arrayInfo)
    {
    }

    /// <summary>
    /// Allocates an array of <typeparamref name="T"/> and fills it with the data provided in the serialized records (in case of primitive types like <see cref="string"/> or <see cref="int"/>) or the serialized records themselves.
    /// </summary>
    /// <param name="allowNulls">Specifies whether null values are allowed.</param>
    /// <param name="maxLength">Specifies the max length of an array that can be allocated.</param>
    /// <remarks>
    /// <para>
    /// The array has <seealso cref="Length"/> elements and can be used as a vector of attack.
    /// Example: an array with <seealso cref="Array.MaxLength"/> elements that contains only nulls
    /// takes 15 bytes to serialize and more than 2 GB to deserialize!
    /// </para>
    /// <para>
    /// A new array is allocated every time this method is called.
    /// </para>
    /// </remarks>
    public T?[] Deserialize(bool allowNulls = true, int maxLength = 64_000)
    {
        if (Length > maxLength)
        {
            ThrowHelper.ThrowMaxArrayLength(maxLength, Length);
        }

        return Deserialize(allowNulls);
    }

    protected abstract T?[] Deserialize(bool allowNulls);
}
