## Safe Binary Formatter Payload Reader

[BinaryFormatter.Serialize](https://learn.microsoft.com/dotnet/api/system.runtime.serialization.formatters.binary.binaryformatter.serialize) method accepts two arguments: `Stream serializationStream` and `object graph`. The first is a stream that we call the **payload**  and the latter is the **root object** of the serialization graph.

The Binary Formatter payload consists of serialization records that represent the serialized objects and their metadata. To read the whole payload and get the root object, the user needs to call `static SerializationRecord Read(Stream payload, bool leaveOpen = false)` method (TODO: add option bag when we have it).

```cs
SerializationRecord rootObject = SafePayloadReader.Read(payload);
```

There is more than a dozen of different serialization [record types](https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/954a0657-b901-4813-9398-4ec732fe8b32), but this library provides a set of abstractions, so the users need to learn only a few of them.

`SerializationRecord` is a base type that exposes the information about the exact `RecordType`.

```cs
public class SerializationRecord
{
    public RecordType RecordType { get; }

    public bool IsSerializedInstanceOf(Type type);
}
```

And helper method that allows to check whether the serialized type name information matches the provided `Type`. (TODO: change the method name, as it checks only type and assembly name, not the member names and their types).

```cs
SerializationRecord rootObject = SafePayloadReader.Read(payload);
if (!rootObject.IsSerializedInstanceOf(typeof($ExpectedType)))
{
    throw new Exception("The payload contains unexpected data!");
}
```

The most important type that derives from `SerializationRecord` is `ClassRecord` which represents **all `class` and `struct` instances beside arrays and primitive types** (`string`, `bool`, `char`, `byte`, `sbyte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`, `float`, `double`, `decimal`, `TimeSpan`, `DateTime`).

```cs
public class ClassRecord : SerializationRecord
{
    public string TypeName { get; }
    public IEnumerable<string> MemberNames { get; }
    public object? this[string memberName] { get; } // the indexer
}
```

`ClassRecord` **indexer** allows you to read the serialized member values and they are returned as an `object?` instance:
- For **primitive types, their values are being returned**.
- For **`null`, it's of course just a `null`**. 
- For **arrays, an instance of `ArrayRecord`**.
- For **other `class` and `struct` instances, it's `ClassRecord`**.


```cs
[Serializable]
public class ComplexType
{
    public int Integer;
    public string? Text;
}

using MemoryStream payload = new();
ComplexType input = new()
{
    Integer = 123,
    Text = "Hello, World!"
};

new BinaryFormatter().Serialize(payload, input);
payload.Position = 0;

ClassRecord rootRecord = SafePayloadReader.ReadClassRecord<ComplexType>(payload);
ComplexType output = new()
{
    // using the indexer to read serialized primitive values
    Integer = rootRecord[nameof(ComplexType.Integer)] is int value ? value : default,
    Text = rootRecord[nameof(ComplexType.Text)] as string, 
};

Console.WriteLine($"{output.Integer}, {output.Text}");
```

As mentioned eariler, the arrays are represented as `ArrayRecord`. It's a base type for all possible array types:
- single dimension (example: `int[]`),
- jagged (example: `int[][]`) 
- rectangular (example: `int[,]`).

```cs
public class ArrayRecord : SerializationRecord
{
    public uint Length { get; }
    public int Rank { get; }
    public ArrayType ArrayType {get; 

    public Array ToArray(Type expectedArrayType, bool allowNulls = true, int maxLength = 64_000)};
}
```

Since single dimension and zero-indexed arrays are expected to be the most common case, the library provides an `ArrayRecord<T>` abstraction, which can be used to represent an array of primitive types and `ClassRecord`s.


```cs
public class ArrayRecord<T> : ArrayRecord
{
    public T?[] ToArray(bool allowNulls = true, int maxLength = 64_000);
}
```

It provides a strongly-types `ToArray` method overload that can help you to materialize an array of primitive types or class records.

```cs
[Serializable]
public class MoreComplexType
{
    public byte[]? Bytes;
}

using MemoryStream payload = new();
MoreComplexType input = new()
{
    Bytes = [0, 1, 2, 3]
};

new BinaryFormatter().Serialize(payload, input);
payload.Position = 0;

ClassRecord rootRecord = SafePayloadReader.ReadClassRecord<MoreComplexType>(payload);
MoreComplexType output = new()
{
    Bytes = rootRecord[nameof(MoreComplexType.Bytes)] is ArrayRecord<byte> byteArray ? byteArray.ToArray() : default,
};

Console.WriteLine($"{string.Join(",", output.Bytes!)}");
```

If you are using jagged or multi-dimensional arrays, you can use the `ToArray` method provided by the base `ArrayRecord` type. To ensure you don't materialize something that you are not expecting (example: an array of a max size full of nulls that takes 2GB to deserialize and just 16 bytes to serialize), you need to specify the expected array type as an argument and cast it back to given array.

```cs
using MemoryStream payload = new();
string[][]? input =
[
    ["a", "b"],
    ["c", "d"]
];

new BinaryFormatter().Serialize(payload, input);
payload.Position = 0;

ArrayRecord arrayRecord = (ArrayRecord)SafePayloadReader.Read(payload);
string[][] jaggedArray = (string[][])arrayRecord.ToArray(expectedArrayType: typeof(string[][]));
foreach (string[] array in jaggedArray)
{
    Console.WriteLine($"{string.Join(",", array)}");
}
```

In case of an array of complex types, the result should be casted to an array of `ClassRecord`:

```cs
MyCustomType[,] input = new MyCustomType[3, 5];

// init, serialize and all of that (skipped for brevity)

ClassRecord?[,] output = (ClassRecord?[,])arrayRecord.ToArray(typeof(MyCustomType[,]));
```









