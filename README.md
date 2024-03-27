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
    public string LibraryName { get; }
    public IEnumerable<string> MemberNames { get; }
    
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
    public object? GetObject(string memberName);

    // Retrieves an array for the provided memberName, with default max length
    public string?[]? GetArrayOfStrings(string memberName, bool allowNulls = true, int maxLength = 64000)
    public T[]? GetArrayOfPrimitiveType<T>(string memberName, int maxLength = 64000) where T : unmanaged;
    public object?[]? GetArrayOfObjects(string memberName, bool allowNulls = true, int maxLength = 64000);

    // Retrieves an instance of ClassRecord that describes non-primitive type for the provided memberName
    public ClassRecord? GetClassRecord(string memberName);
    // Retrieves an array of ClassRecords
    public ClassRecord?[]? GetArrayOfClassRecords(string memberName, bool allowNulls = true, int maxLength = 64000);
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

ClassRecord rootRecord = PayloadReader.ReadExactClassRecord<Sample>(payload);
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





