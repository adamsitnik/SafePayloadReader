# Safe Binary Format Payload Reader

## Goal

The goal of this library is to allow for safe reading of [Binary Format](https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/75b9fe09-be15-475f-85b8-ae7b7558cfe5) payload from untrusted input.

The principles:
- Treating every input as potentially hostile.
- No type loading of any kind (to avoid remote code execution).
- No recursion of any kind (to avoid unbound recursion, stack overflow and denial of service).
- No buffer pre-allocation based on size provided in payload (to avoid running out of memory and denial of service).
- Using collision-resistant dictionary to store records referenced by other records.
- Only primitive types can be instantiated in implicit way. Arrays can be instantiated on demand (with a default max size limit). Other types are never instantiated.

## API

[BinaryFormatter.Serialize](https://learn.microsoft.com/dotnet/api/system.runtime.serialization.formatters.binary.binaryformatter.serialize) method accepts two arguments: `Stream serializationStream` and `object graph`. The first is a stream that we call the **payload**  and the latter is the **root object** of the serialization graph.

The Binary Formatter payload consists of serialization records that represent the serialized objects and their metadata. To read the whole payload and get the root object, the user need to call `static SerializationRecord Read(Stream payload, PayloadOptions? options = null, bool leaveOpen = false)` method. There is more than a dozen of different serialization [record types](https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/954a0657-b901-4813-9398-4ec732fe8b32), but this library provides a set of abstractions, so the users need to learn only a few of them:
- `PrimitiveTypeRecord<T>` that describes all primitive types natively supported by the Binary Format (`string`, `bool`, `char`, `byte`, `sbyte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`, `float`, `double`, `decimal`, `TimeSpan`, `DateTime`)
- `ClassRecord` that describes all `class` and `struct`  beside the formentioned primitive types.
- `ArrayRecord<T>` that describes single-dimension array records, where `T` can be either a primitive type or `ClassRecord`.
- `ArrayRecord` that describes all array records including jagged and multi-dimensional arrays.

```cs
SerializationRecord rootObject = PayloadReader.Read(payload);

if (rootObject is PrimitiveTypeRecord<string> stringRecord)
{
    Console.WriteLine($"It was a string: '{stringRecord.Value}'");
}
else if (rootObject is ClassRecord classRecord)
{
    Console.WriteLine($"It was a class record of '{classRecord.TypeName}' type.");
}
else if (rootObject is ArrayRecord<byte> arrayOfBytes)
{
    Console.WriteLine($"It was an array of bytes: '{string.Join(",", arrayOfBytes.ToArray())}'");
}
```

Beside `Read`, the `PayloadReader` exposes a `ReadClassRecord` method that returns `ClassRecord` (or throws) and. It also provides two `ContainsBinaryFormatterPayload` methods that allow to **check whether given stream or buffer contains binary formatter payload**.

### ClassRecord

The most important type that derives from `SerializationRecord` is `ClassRecord` which represents **all `class` and `struct` instances beside arrays and selected primitive types**.

```cs
public class ClassRecord : SerializationRecord
{
    public TypeName TypeName { get; }
    public IEnumerable<string> MemberNames { get; }

    // Checks if member of given name was present in the payload (useful for versioning scenarios)
    public bool HasMember(string memberName);
    
    // Retrieves the value of the provided memberName
    public string? GetString(string memberName);
    public bool GetBoolean(string memberName);
    public byte GetByte(string memberName);
    public sbyte GetSByte(string memberName);
    public short GetInt16(string memberName);
    public ushort GetUInt16(string memberName);
    public char GetChar(string memberName);
    public int GetInt32(string memberName);
    public uint GetUInt32(string memberName);
    public float GetSingle(string memberName);
    public long GetInt64(string memberName);
    public ulong GetUInt64(string memberName);
    public double GetDouble(string memberName);
    public decimal GetDecimal(string memberName);
    public TimeSpan GetTimeSpan(string memberName);
    public DateTime GetDateTime(string memberName);
    public object? GetRawValue(string memberName);

    // Retrieves an array for the provided memberName, with default max length
    public T[]? GetArrayOfPrimitiveType<T>(string memberName, int maxLength = 64000);

    // Retrieves an instance of ClassRecord that describes non-primitive type for the provided memberName
    public ClassRecord? GetClassRecord(string memberName);
    // Retrieves any other serialization record like jagged array or array of complex types
    public SerializationRecord? GetSerializationRecord(string memberName);
}
```

`Get$PrimitiveType` methods read a value of given primitive type.
`GetArrayOfPrimitiveType<T>` methods read arrays of values of given primitive type.
`GetClassRecord` method reads an instance of `ClassRecord` that describes non-primitive type like a custom `class` or `struct`.

```cs
[Serializable]
public class Sample
{
    public int Integer;
    public string? Text;
    public byte[]? ArrayOfBytes;
    public Sample? ClassInstance;
}

ClassRecord rootRecord = PayloadReader.ReadClassRecord(payload);
Sample output = new()
{
    // using the dedicated methods to read primitive values
    Integer = rootRecord.GetInt32(nameof(Sample.Integer)),
    Text = rootRecord.GetString(nameof(Sample.Text)),
    // using dedicated method to read an array of bytes
    ArrayOfBytes = rootRecord.GetArrayOfPrimitiveType<byte>(nameof(Sample.ArrayOfBytes)),
    // using GetClassRecord to read a class record
    ClassInstance = new()
    {
        Text = rootRecord
            .GetClassRecord(nameof(Sample.ClassInstance))!
            .GetString(nameof(Sample.Text))
    }  
};
```

TODO: describe how to work with Jagged and Rectangular arrays
