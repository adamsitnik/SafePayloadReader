using System.Collections.Generic;

namespace System.Runtime.Serialization.BinaryFormat;

internal readonly struct NextInfo
{
    internal NextInfo(AllowedRecordTypes allowed, SerializationRecord parent,
        Stack<NextInfo> stack, PrimitiveType primitiveType = default)
    {
        Allowed = allowed;
        Parent = parent;
        Stack = stack;
        PrimitiveType = primitiveType;
    }

    internal AllowedRecordTypes Allowed { get; }

    internal SerializationRecord Parent { get; }

    internal Stack<NextInfo> Stack { get; }

    internal PrimitiveType PrimitiveType { get; }

    internal NextInfo With(AllowedRecordTypes allowed, PrimitiveType primitiveType)
        => allowed == Allowed && primitiveType == PrimitiveType
            ? this // previous record was of the same type
            : new(allowed, Parent, Stack, primitiveType);
}
