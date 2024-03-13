using System.Collections.Generic;

namespace System.Runtime.Serialization.BinaryFormat;

public sealed class JaggedArrayRecord<T> : ArrayRecord
{
    internal JaggedArrayRecord(ArrayInfo arrayInfo, MemberTypeInfo memberTypeInfo)
        : base(arrayInfo)
    {
        MemberTypeInfo = memberTypeInfo;
        Values = new();
    }

    public override Type ElementType => typeof(T);

    public override RecordType RecordType => RecordType.BinaryArray;

    private MemberTypeInfo MemberTypeInfo { get; }

    private List<object> Values { get; }

    private protected override Array Deserialize(bool allowNulls, int maxLength)
        => ToArray(allowNulls, maxLength);

    public T?[][] ToArray(bool allowNulls = true, int maxLength = 64_000)
    {
        if (Length > maxLength)
        {
            ThrowHelper.ThrowMaxArrayLength(maxLength, Length);
        }

        T?[][] result = new T?[Length][];
        for (int i = 0; i < result.Length; i++)
        {
            object item = Values[i];

            if (item is MemberReferenceRecord referenceRecord)
            {
                item = referenceRecord.GetReferencedRecord();
            }

            if (item is ArrayRecord<T> arrayRecord)
            {
                result[i] = arrayRecord.ToArray(allowNulls, maxLength);
                continue;
            }

            if (!allowNulls)
            {
                ThrowHelper.ThrowArrayContainedNull();
            }
            // TODO: handle multiple nulls?
        }

        return result;
    }

    internal override (AllowedRecordTypes allowed, PrimitiveType primitiveType) GetAllowedRecordType()
    {
        (AllowedRecordTypes allowed, PrimitiveType primitiveType) = MemberTypeInfo.GetNextAllowedRecordType(0);

        if (allowed != AllowedRecordTypes.None)
        {
            // It's an array, it can also contain multiple nulls
            return (allowed | AllowedRecordTypes.Nulls, primitiveType);
        }

        return (allowed, primitiveType);
    }

    private protected override void AddValue(object value) => Values.Add(value);

    private protected override bool IsElementType(Type typeElement)
        => MemberTypeInfo.IsElementType(typeElement);
}
