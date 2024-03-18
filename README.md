# Safe Binary Format Payload Reader

## Goal

The goal of this library is to allow for safe reading of [Binary Format](https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/75b9fe09-be15-475f-85b8-ae7b7558cfe5) payload from untrusted input.

The principles:
- Treating every input as hostile.
- No type loading of any kind (to avoid remote code execution).
- No recursion of any kind (to avoid unbound recursion, stack overflow and denial of service).
- No buffer pre-allocation based on size provided in payload (to avoid running out of memory and denial of service).
- Using collision-resistant dictionary to store records referenced by other records.
- Only primitive types can be instantiated in implicit way. Arrays can be instantiated on demand (with a default max size limit). Other types are never instantiated.

## API

[BinaryFormatter.Serialize](https://learn.microsoft.com/dotnet/api/system.runtime.serialization.formatters.binary.binaryformatter.serialize) method accepts two arguments: `Stream serializationStream` and `object graph`. The first is a stream that we call the **payload**  and the latter is the **root object** of the serialization graph.

The Binary Formatter payload consists of serialization records that represent the serialized objects and their metadata. To read the whole payload and get the root object, the user can call `static SerializationRecord Read(Stream payload, bool leaveOpen = false)` method (TODO: add option bag when we have it).

```cs
SerializationRecord rootObject = PayloadReader.Read(payload);
```

There is more than a dozen of different serialization [record types](https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/954a0657-b901-4813-9398-4ec732fe8b32), but this library provides a set of abstractions, so the users need to learn only a few of them.

`SerializationRecord` is a base type that exposes information about the exact `RecordType`.

```cs
public class SerializationRecord
{
    public RecordType RecordType { get; }

    public bool IsTypeNameMatching(Type type);
}
```

And a helper method that allows to check whether the serialized type name information matches the provided `Type` (it takes type forwarding into acount).

```cs
SerializationRecord rootObject = PayloadReader.Read(payload);
if (!rootObject.IsTypeNameMatching(typeof($ExpectedType)))
{
    throw new Exception("The payload contains unexpected data!");
}
```

Beside `Read`, the `PayloadReader` exposes a set of dedicated methods for reading exact records:
- `ReadString` and `ReadArrayOfStrings`
- `ReadPrimitiveType<T>` and `ReadArrayOfPrimitiveType<T>`
- `ReadAnyClassRecord`, `ReadExactClassRecord<T>` (it performs the check `IsTypeNameMatching` shown above)), `ReadArrayOfAnyClassRecords` and `ReadArrayOfExactClassRecords<T>` (it also performs the type name check)
- `ReadAnyArrayRecord` and `ReadArrayOfObjects`

### ClassRecord

The most important type that derives from `SerializationRecord` is `ClassRecord` which represents **all `class` and `struct` instances beside arrays and selected primitive types** (`string`, `bool`, `char`, `byte`, `sbyte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`, `float`, `double`, `decimal`, `TimeSpan`, `DateTime`).

```cs
public class ClassRecord : SerializationRecord
{
    public string TypeName { get; }
    public IEnumerable<string> MemberNames { get; }
    public object? this[string memberName] { get; } // the indexer
}
```

`ClassRecord` **indexer** allows you to read the serialized member values and they are returned as an `object?` instance:
- For primitive types, returns their value.
- For nulls, returns a `null`. 
- For other types that are not arrays, returns an instance of `ClassRecord`.
- For single-dimensional arrays returns `ArrayRecord<T>` where `T` is primivite type or `ClassRecord`.
- For jagged and multi-dimensional arrays, returns an instance of `ArrayRecord`.

```cs
ClassRecord rootRecord = PayloadReader.ReadExactClassRecord<ComplexType>(payload);
ComplexType output = new()
{
    // using the indexer to read serialized primitive values
    Integer = rootRecord[nameof(ComplexType.Integer)] is int value ? value : default,
    Text = rootRecord[nameof(ComplexType.Text)] as string, 
};
```

### ArrayRecord


As mentioned earlier, the arrays are represented as `ArrayRecord`. It's a base type for all possible array types:
- single dimension (example: `int[]`),
- jagged (example: `int[][]`) 
- multi-dimensional (example: `int[,]`).

```cs
public class ArrayRecord : SerializationRecord
{
    public uint Length { get; }
    public int Rank { get; }
    public ArrayType ArrayType {get; 

    public Array ToArray(Type expectedArrayType, bool allowNulls = true, int maxLength = 64_000)};
}
```

#### Single dimension

Since single dimension and zero-indexed arrays are expected to be the most common case, the library provides an `ArrayRecord<T>` abstraction, which can be used to represent an array of primitive types and `ClassRecord`s.


```cs
public class ArrayRecord<T> : ArrayRecord
{
    public T?[] ToArray(bool allowNulls = true, int maxLength = 64_000);
}
```

It provides a strongly-typed `ToArray` method overload that can help you to materialize an array of primitive types or class records.

```cs
// public class WithArray { public byte[]? ArrayOfBytes; }

ClassRecord rootRecord = PayloadReader.ReadExactClassRecord<WithArray>(payload);
WithArray output = new()
{
    ArrayOfBytes = rootRecord[nameof(WithArray.ArrayOfBytes)] is ArrayRecord<byte> byteArray 
        ? byteArray.ToArray() : default,
};
```

#### Jagged and multi-dimensional

If you are using jagged or multi-dimensional arrays, you can use the `ToArray` method provided by the base `ArrayRecord` type. To ensure you don't materialize something that you are not expecting (example: an array of a max size full of nulls that takes 2GB to deserialize and just 16 bytes to serialize), you need to specify the expected array type as an argument and cast it back to given array.

```cs
ArrayRecord arrayRecord = PayloadReader.ReadAnyArrayRecord(payload);
string[][] jaggedArray = (string[][])arrayRecord.ToArray(expectedArrayType: typeof(string[][]));
```

In case of an array of complex types, the result should be casted to an array of `ClassRecord`:

```cs
ClassRecord?[,] output = (ClassRecord?[,])arrayRecord.ToArray(typeof(MyCustomType[,]));
```









