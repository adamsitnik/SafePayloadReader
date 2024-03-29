# Binary Formatter migration guide

- [BinaryFormatter Deprecation and Removal](#binaryformatter-deprecation-and-removal)
- [Choosing different serializer](#choosing-different-serializer)
   * [XML](#xml)
   * [JSON](#json)
   * [Binary](#binary)
- [Persisted Payloads](#persisted-payloads)
   * [Identifying if a payload was created using BinaryFormatter](#identifying-if-a-payload-was-created-using-binaryformatter)
   * [Safely reading persisted BinaryFormatter payloads](#safely-reading-persisted-binaryformatter-payloads)
   * [Deserializing a closed set of types](#deserializing-a-closed-set-of-types)
- [Migrations](#migrations)
   * [DataContractSerializer](#datacontractserializer)
   * [System.Text.Json](#systemtextjson)
   * [Messagepack](#messagepack)
- [WinForms embedded resources](#winforms-embedded-resources)
- [WPF embedded resources](#wpf-embedded-resources)
- [Using the Compatibility Package](#using-the-compatibility-package)
   * [Improving security through allow lists](#improving-security-through-allow-lists)
- [BinaryFormatter functionality reference](#binaryformatter-functionality-reference)
   * [Member names](#member-names)
   * [Serialization Binder](#serialization-binder)
- [Summary](#summary)

## BinaryFormatter Deprecation and Removal

Ever since .NET Core 1.0, we in .NET Security have been trying to lay BinaryFormatter to rest. It’s long been known by security practitioners that any deserializer, binary or text, which allows its input to carry information about the objects to be created is a security problem waiting to happen. There is even a Common Weakness Enumeration (CWE) that describes the issue, [CWE-502 “Deserialization of Untrusted Data”](https://cwe.mitre.org/data/definitions/502.html) and examples of this type of vulnerability run from security issues in Exchange through security issues in Apache. Within Microsoft the use of BinaryFormatter with untrusted input has caused many instances of heartache, late nights, and weekend work trying to produce a solution.

In .NET Core 1.0 we removed BinaryFormatter entirely due to its known risks, but without a clear path to using something safer customer demand brought it back to .NET Core 1.1. Since then, we have been on the path to removal, slowly turning it off by default in multiple project types but letting you opt-in via flags if you still needed it for backward compatibility. .NET 9 sees the culmination of this effort with the removal of BinaryFormatter. In .NET 9 these flags will no longer exist and the in-box implementation of BinaryFormatter will throw exceptions in any project type when you try to use it. However, if you are committed to using a class that cannot be made secure you will still be able to.

If you want to find out more details about our decision please read [BinaryFormatter is being removed in .NET 9](https://github.com/dotnet/announcements/issues/293) announcment. If you want to share some feedback, please leave a comment in [the discussion issue](https://github.com/dotnet/runtime/issues/98245).

## Choosing different serializer

Choosing different serializer boils down to two questions:

- Is compact binary representation important for your scenario? If so, you need to switch to a different binary serializer. If not, you can consider using JSON and XML serializers.
- Can you modify the types that are being serialized by annotating them with attributes, adding new constructors, making the types public and changing fields to properties? If not, using the modern serializers might require more extra work (like implementing custom converters or resolvers).

| Feature                                        | BinaryFormatter | DataContractSerializer | System.Text.Json        | MessagePack              |
|------------------------------------------------|-----------------|------------------------|-------------------------|--------------------------|
| Compact binary representation                  | ✔️              | ❌                      | ❌                      |  ✔️                       |
| Human readable                                 | ❌️              | ✔                      | ✔                      |  ❌                       |
| Performance                                    | ❌️              | ❌                      | ✔️                      |  ✔️✔️                     |
| `[Serializable]` support                       | ✔️              | ✔️                      | ❌                      |  ❌                       |
| Serializing public types                       | ✔️              | ✔️                      | ✔️                      |  ✔️                       |
| Serializing non-public types                   | ✔️              | ✔️                      | ✔️                      |  ❌                       |
| Serializing fields                             | ✔️              | ✔️                      | ✔️ (opt in)             |  ✔️ (attribute required)  |
| Serializing properties                         | ✔️<sup>*</sup>  | ✔️                      | ✔️                      |  ✔️ (attribute required)  |
| Deserializing readonly members                 | ✔️              | ✔️                      | ✔️ (attribute required) |  ✔️                       |
| Polymorphic type hierarchy    | ✔️              | ✔️                      | ✔️ (attribute required) |  ✔️ (attribute required)  |

### XML

The .NET base class libraries provide two XML serializers [XmlSerializer](https://learn.microsoft.com/en-us/dotnet/standard/serialization/introducing-xml-serialization) and [DataContractSerializer](https://learn.microsoft.com/dotnet/fundamentals/runtime-libraries/system-runtime-serialization-datacontractserializer). There are some subtle differences between these two, but for the purpose of the migration we are going to focus only on `DataContractSerializer`. Why? Because it **fully supports the serialization programming model that was used by the `BinaryFormatter`**. So all the types that are already marked as `[Serializable]` and/or implement `ISerializable` can be serialized with `DataContractSerializer`. Where is the catch? Known types must be specified up-front (that is why it's secure). So you need to know them and be able to get the `Type` **even for private types**.

```cs
DataContractSerializer serializer = new(
    type: input.GetType(), 
    knownTypes: new Type[] 
    {
        typeof(MyType1),
        typeof(MyType2)
    });
```

It's not required to specify most popular collections or primitive types like `string` or `DateTime` (the serializer has it's own default allowlist), but there are exceptions like `DateTimeOffset`. You can read about the supported types in the [dedicated doc](https://learn.microsoft.com/dotnet/framework/wcf/feature-details/types-supported-by-the-data-contract-serializer).

[Partial trust](https://learn.microsoft.com/dotnet/framework/wcf/feature-details/partial-trust) is a Full .NET Framework feature that was not ported to .NET (Core). If your code runs on a Full .NET Framework and uses this feature, please read about the [limitations](https://learn.microsoft.com/dotnet/framework/wcf/feature-details/types-supported-by-the-data-contract-serializer#limitations-of-using-certain-types-in-partial-trust-mode) that may apply to such scenario.

### JSON

[System.Text.Json](https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/overview) is strict by default and avoids any guessing or interpretation on the caller's behalf, emphasizing deterministic behavior. The library is intentionally designed this way for performance and security. From the migration perspective, it's crucial to know the following facts:
- By default, **fields aren't serialized**, but they can be [included on demand](https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/fields), which is a must-have for types that use fields that are not exposed by properties. The simplest solution that does not require modifying the types is to use the global setting to include fields.
```cs
JsonSerializerOptions options = new()
{
    IncludeFields = true
};
```
- By default, System.Text.Json **ignores private fields and properties**. You can enable use of a non-public accessor on a property by using the `[JsonInclude]` attribute. Including private fields requires some [non-trivial extra work](https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/custom-contracts#example-serialize-private-fields).
- It **[can not deserialize readonly fields](https://learn.microsoft.com/dotnet/api/system.text.json.jsonserializeroptions.ignorereadonlyfields?view#remarks)** or properties, but `[JsonConstructor]` attribute can be used to indicate that given constructor should be used to create instances of the type on deserialization. And obviously the constructor can set the readonly fields and properties.
- It [supports serialization and deserialization of most built-in collections](https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/supported-collection-types). The exceptions:
    - multi-dimensional arrays,
    - `BitArray`,
    - `LinkedList<T>`,
    - `Dictionary<TKey, TValue>`, where `TKey` is not a primitive type,
    - `BlockingCollection<T>` and `ConcurrentBag<T>`,
    - most of the collections from [System.Collections.Specialized](https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/supported-collection-types#systemcollectionsspecialized-namespace) and [System.Collections.ObjectModel](https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/supported-collection-types#systemcollectionsobjectmodel-namespace) namespaces.
- Under [certain condtions](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/supported-collection-types#custom-collections-with-deserialization-support), it supports serialization and deserialization of custom generic collections.
- Other types [without built-in support](https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/migrate-from-newtonsoft?pivots=dotnet-9-0#types-without-built-in-support) are: `DataSet`, `DataTable`, `DBNull`, `TimeZoneInfo`, `Type`, `ValueTuple`. However, you can write a custom converter to support these types. 
- It [supports polymorphic type hierarchy serialization and deserialization](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/polymorphism) that have been explicitly opted in via the `[JsonDerivedType]` attribute or via custom resolver.
- The `[JsonIgnore]` attribute on a property causes the property to be omitted from the JSON during serialization.
- To preserve references and handle circular references in System.Text.Json, set `JsonSerializerOptions.ReferenceHandler` to `Preserve`.
- To override the default behavior you can [write custom converters](https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/converters-how-to).

### Binary

.NET Team is deprecating the `BinaryFormatter`, but at the same time we currently have no plans to implement a new binary serializer. It puts us in a situation, where we can't recommend any serializer that we own. But luckily for all of us, the .NET Open Source Ecosystem provides many great binary serializers. Some of them:
- [protobuf-net](https://github.com/protobuf-net/protobuf-net) is "a contract based serializer for .NET code, that happens to write data in the "protocol buffers" serialization format engineered by Google".
- [Bond](https://github.com/Microsoft/bond) is "a cross-platform framework for working with schematized data. It supports cross-language de/serialization". It's fair to say that it's Microsoft’s equivalent of Google’s Protocol Buffers.

Both serializers are a .NET implementation of cross-language protocol. Because of that, they have some subtle issues with .NET-specific concepts like [nulls and empty arrays](https://stackoverflow.com/questions/21631428/protobuf-net-deserializes-empty-collection-to-null-when-the-collection-is-a-prop/21632160#21632160). In some scenarios they require to specify the types in a platform-independent way ([example](https://microsoft.github.io/bond/manual/bond_cs.html): string with a length prefix vs string with a null character at the end). Since this document is about a migration from `BinaryFormatter`, we can safely assume that we don't need cross-language support and the complexity it brings. That is why in this particular scenario we recommend MessagePack.

MessagePack provides a highly efficient binary serialization format, resulting in smaller message sizes compared to JSON and XML. It's very [performant](https://github.com/MessagePack-CSharp/MessagePack-CSharp?tab=readme-ov-file#performance), ships with built-in support for LZ4 compression and a full set of general-purpose expressive data types:

- **Only public types are supported!**
- By default, MessagePack requires each serializable type to be annotated with `[MessagePackObject]` attribute. It's possible to avoid that by using the [ContractlessStandardResolver](https://github.com/MessagePack-CSharp/MessagePack-CSharp?tab=readme-ov-file#object-serialization), but it may cause issues with versioning in the future.
- Every serializable non-static field and a property needs to be annotated with `[Key]` attribute. If you annotate the type with `[MessagePackObject(keyAsPropertyName: true)]` attribute, then members do not require explicit annotations. In such case, to ignore certain public members the `[IgnoreMember]` attribute needs to be used.
- To serialize private members, use [DynamicObjectResolverAllowPrivate](https://github.com/MessagePack-CSharp/MessagePack-CSharp?tab=readme-ov-file#object-serialization).
- `System.Runtime.Serialization` annotations can be used instead of MessagePack annotations. `[DataContract]` instead of`[MessagePackObject]`, `[DataMember]` instead of `[Key]` and `[IgnoreDataMember]` instead of `[IgnoreMember]`. It can be very useful if you want to avoid having dependency on MessagePack in the library that defines serializable types. (TODO: try to port Quartz by using these annotations).
- It supports readonly/immutable types and members. The serializer will try to use the public constructor with the best matched argument list. It can be specified in an explicit way by using `[SerializationConstructor]` attribute.
- The serializer supports most frequently used built-in types and collections provided by the .NET base class libraries. You can find the full list in [official docs](https://github.com/MessagePack-CSharp/MessagePack-CSharp?tab=readme-ov-file#built-in-supported-types). It has [extension points](https://github.com/MessagePack-CSharp/MessagePack-CSharp?tab=readme-ov-file#extensions) that allow for customization.
- The library provides [Typless API](https://github.com/MessagePack-CSharp/MessagePack-CSharp?tab=readme-ov-file#typeless) similar to `BinaryFormatter`, but it should not be used as it's not [secure](https://github.com/MessagePack-CSharp/MessagePack-CSharp?tab=readme-ov-file#security) and would defeat the purpose of migrating from `BinaryFormatter`.

## Persisted Payloads

**Do you own any data that was serialized with `BinaryFormatter` and persisted**? Sample scenarios that typically don't include persisting the payload:
- Deep object cloning (allocate a stream, serialize to it, seek to the beginning of the stream, deserialize from it and return deserialized instance).
- Testing whether given type is serializable.

A scenario where it depends, is inter-process communication. If you own all the processes and can migrate them at the same time, you don't need to read the binary format payload. However, if you own only one side (example: you expose a backend that accepts requests from 3rd party clients), or you can not shut down all the processes to perform the migration, you need to read it.

**If you don't need to read the payload, you can skip this paragraph**.

### Identifying if a payload was created using BinaryFormatter

For now, we can simplify the process and assume that for the time of migration from `BinaryFormatter`, the app should do the following:

- Check if the payload read from storage is a binary formatter payload.
- If so, read it with `PayloadReader`, serialize back with a new serializer and overwrite the data in the storage.
- If not, use the new serializer to deserialize the data.

`PayloadReader` provides two `ContainsBinaryFormatterPayload` methods that allow to **check whether given stream or buffer contains binary formatter payload**. The `Stream` overload resets the stream position to the initial value, but both more or less check the first and last bytes.

```cs
static T Pseudocode<T>(Stream payload, NewSerializer newSerializer)
{
    if (PayloadReader.ContainsBinaryFormatterPayload(payload))
    {
        T fromPayload = UseThePayloadReaderToReadTheData<T>(payload);

        payload.Seek(0, SeekOrigin.Begin);
        newSerializer.Serialize(payload, fromPayload);
        payload.Flush();
    }
    else
    {
        return newSerializer.Deserialize<T>(payload)
    }
}
```

### Safely reading persisted BinaryFormatter payloads

`PayloadReader` can read any payload that was serialized with `BinaryFormatter` (except of types specific to **remoting** which were never ported to .NET (Core)). 

`PayloadReader` is following these principles to read from **untrusted input**:
- Treating every input as potentially hostile.
- No type loading of any kind (to avoid remote code execution).
- No recursion of any kind (to avoid unbound recursion, stack overflow and denial of service).
- No buffer pre-allocation based on size provided in payload (to avoid running out of memory and denial of service).
- Using collision-resistant dictionary to store records referenced by other records.
- Only primitive types can be instantiated in implicit way. Arrays can be instantiated on demand (with a default max size limit). Other types are never instantiated.

The Binary Formatter payload consists of serialization records that represent the serialized objects and their metadata. To read the whole payload and get the root object, the user need to call `static SerializationRecord Read(Stream payload, bool leaveOpen = false)` method (TODO: add option bag when we have it). There is more than a dozen of different serialization [record types](https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/954a0657-b901-4813-9398-4ec732fe8b32), but this library provides a set of abstractions, so the users need to learn only a few of them:
- `PrimitiveTypeRecord<T>` that describes all primitive types natively supported by the Binary Format (`string`, `bool`, `char`, `byte`, `sbyte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`, `float`, `double`, `decimal`, `TimeSpan`, `DateTime`)
- `ClassRecord` that describes all `class` and `struct`  beside the formentioned primitive types.
- `ArrayRecord<T>` that describes single-dimension array records, where `T` can be either a primitive type or `ClassRecord`.
- `ArrayRecord` that describes all array records including jagged and multi-dimensional arrays.

```cs
SerializationRecord rootObject = PayloadReader.Read(payload); // payload is a Stream

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

Beside `Read`, the `PayloadReader` exposes a `ReadClassRecord` method that returns `ClassRecord` (or throws).

The most important type that derives from `SerializationRecord` is `ClassRecord` which represents **all `class` and `struct` instances beside arrays and selected primitive types**.

```cs
public class ClassRecord : SerializationRecord
{
    public string TypeName { get; }
    public string LibraryName { get; }
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
    public object? GetObject(string memberName);

    // Retrieves an array for the provided memberName, with default max length
    public T[]? GetArrayOfPrimitiveType<T>(string memberName, int maxLength = 64000) where T : unmanaged;

    // Retrieves an instance of ClassRecord that describes non-primitive type for the provided memberName
    public ClassRecord? GetClassRecord(string memberName);
    // Retrieves any other serialization record like jagged array or array of complex types
    public SerializationRecord? GetSerializationRecord(string memberName);
}
```

`Get$PrimitiveType` methods read a value of given primitive type.
`GetArrayOfPrimitiveType<T>` methods read arrays of values of given primitive type.
`GetClassRecord` method reads an instance of `ClassRecord` that describes non-primitive type like a custom `class` or `struct`.

### Deserializing a closed set of types

`PayloadReader` is mostly useful only when the list of serialized types is a known, closed set. Or put it otherwise, you need to know up front what you want to read, because at the end of the day you also need to create instances of these types and populate them with data that was read from the payload. Let's consider two opposite examples.

All `[Serializable]` types from [Quartz.NET](https://github.com/search?q=repo%3Aquartznet%2Fquartznet+%5BSerializable%5D+language%3AC%23&type=code&l=C%23) that can be persisted by the library itself are `sealed`, so there are no custom types that the users could create and the payload can contain only known types. They also provide public constructors, so it is possible to re-create these types based on the information read from payload.

`SettingsPropertyValue` type from `System.Configuration.ConfigurationManager` library exposes an `object PropertyValue` that may internally use `BinaryFormatter` to serialize and deserialize any object that was stored in the configuration file. It could be used to store an integer, a custom type, a dictionary or literally anything.  Because of that, **it is impossible to migrate this library without introducing breaking changes to the API, as the payload can contain anything**.

```cs
[Serializable]
public sealed class TimeOfDay
{
    public readonly int Hour, Minute, Second;

    public TimeOfDay(int hour, int minute, int second);
}

[Serializable]
public sealed class CronExpression : ISerializable
{
    private readonly string cronExpression;

    public CronExpression(string cronExpression);
}

private static T Deserialize<T>(Stream payload)
{
    ClassRecord rootRecord = PayloadReader.ReadClassRecord(payload);
    if (rootRecord.IsTypeNameMatching(typeof(T)))
    {
        throw new InvalidOperationException("Payload contained unexpected data");
    }

    if (rootRecord.IsTypeNameMatching(typeof(TimeOfDay)))
    {
        return (T)(object)new TimeOfDay(
            rootRecord.GetInt32("Hour"),
            rootRecord.GetInt32("Minut"),
            rootRecord.GetInt32("Second")
        );
    }
    else if (rootRecord.IsTypeNameMatching(typeof(CronExpression)))
    {
        return (T)(object)new CronExpression(rootRecord.GetString("cronExpression")!);
    }
    else
    {
        throw new NotSupportedException();
    }    
}
```

## Migrations

### DataContractSerializer

TODO: take one OSS lib, port it and discuss examples here, mostly focusing on the non-obious edge cases, as the overall transition should be very smooth as it also supports `[Serializable]`

### System.Text.Json

TODO: take one OSS lib, port it and discuss examples here

### Messagepack

TODO: take one OSS lib, port it and discuss examples here

## WinForms embedded resources

## WPF embedded resources

## Using the Compatibility Package

TODO: explain where to find and how to use the package, but at the same time warn about it being unsafe

### Improving security through allow lists

TODO: explain how to use the new allowlist serialization binder

## BinaryFormatter functionality reference

The `BinaryFormatter` was first introduced with the initial release of the .NET Framework in 2002. It's very likely that engineers who are assigned to the migration task may not have the necessary knowledge or experience to work with the `BinaryFormatter`. This can lead to errors, delays, or failures.
Therefore, it is crucial for engineers to understand how the old technology works before they start the migration. 

`BinaryFormatter` can serialize any `object` that is annotated with `[Serializable]` attribute or implements `ISerializable` interface.

### Member names

In most common scenario the type is just annotated with `[Serializable]` attribute and the serializer uses reflection to serialize **all fields** (both public and not) that are not annotated with `[NonSerialized]` attribute. In case of C# auto properties, they are backed by fields generated by the C# compiler, so the names of the serialized fields are compiler-generated (and not very human friendly to say politely).

Let's use one of the C# decompilers like [https://sharplab.io/](https://sharplab.io/) or [ILSpy](https://github.com/icsharpcode/ILSpy) to see what field gets generated for following simple property.

```cs
[Serializable]
internal class PropertySample
{
    public string Name { get; set; }
}
```

Is translated by the C# compiler to:

```cs
[Serializable]
internal class PropertySample
{
    private string <Name>k__BackingField;

    public string Name
    {
        get
        {
            return <Name>k__BackingField;
        }
        set
        {
            <Name>k__BackingField = value;
        }
    }
}
```

And in this case `<Name>k__BackingField` is **the name of the member that `BinaryFormatter` uses in the serialized payload**. It's impossible to use `nameof` or any other C# operator to get this name.

[ISerializable](https://learn.microsoft.com/dotnet/api/system.runtime.serialization.iserializable) interface comes with [GetObjectData(SerializationInfo info, StreamingContext context)](https://learn.microsoft.com/dotnet/api/system.runtime.serialization.iserializable.getobjectdata) method that allows the users to control the names, by using one of the [SerializationInfo.AddValue](https://learn.microsoft.com/dotnet/api/system.runtime.serialization.serializationinfo.addvalue) methods.


```cs
// please note lack of any special attribute
public void GetObjectData(SerializationInfo info, StreamingContext context)
{
    info.AddValue("Name", this.Name);
}
```

If such customization has been applied, the information needs to be provided during deserialization as well. It's possible by using the **serialization constructor** where all values are read from `SerializationInfo` by using one of the `Get` methods it provides.


```cs
private PropertySample(SerializationInfo info, StreamingContext context)
{
    this.Name = info.GetString("Name");
}
```

**Note:** The `nameof` operator was not used here on purpose, as the payload can be persisted and the property can get renamed at some point of time. So even if it gets renamed (let's say to `FirstName` because we decided to also introduce `LastName` property), to remain backward compatibility the serialization should still use the old name that could have been persisted somewhere.

### Serialization Binder

On top of that, it's recommended to use [SerializationBinder](https://learn.microsoft.com/dotnet/api/system.runtime.serialization.serializationbinder) to control class loading and mandate what class to load and therefore minimize security vulnerabilities (so only allowed types get loaded, even if the attacker modifies the payload to deserialize and load something else).

Using this type requires inheriting from it and overriding the [Type BindToType(string assemblyName, string typeName)](https://learn.microsoft.com/dotnet/api/system.runtime.serialization.serializationbinder.bindtotype#system-runtime-serialization-serializationbinder-bindtotype(system-string-system-string)) method.

If given codebase uses custom type that derives from `SerializationBinder` it's likely that the list of serializable types is a **closed set**. If not, finding the list of all types that can get serialized and deserialized is going to require studying all the usages of `BinaryFormatter` in source code. **Knowing that list is crucial to determining how to move further with the migration**.

## Summary

* `BinaryFormatter` by default serializes **all fields** that are not marked with `[NonSerialized]` attribute. For auto-properties, it uses names generated by the compiler. It can be customized by implementing  `GetObjectData` method from `ISerializable` interface, but requires to implement a serialization constructor that accepts `SerializationInfo info` and `StreamingContext context` arguments. It also preserves the references and handles circular references.
* `PayloadReader` should be used for reading any `BinaryFormatter` payload that was either persisted before the migration or comes from a service that can not be migrated (example: owned by a 3rd party). This new type can read any Binary Format payload, but it's mandatory to know the list of types that could have been serialized. Otherwise, it's impossible to create and hydrate the serialized objects.
* Is compact binary representation important for your scenario? If so, you need to switch to a different binary serializer. If not, you can consider using XML and JSON serializers.
* [DataContractSerializer](https://learn.microsoft.com/dotnet/fundamentals/runtime-libraries/system-runtime-serialization-datacontractserializer) is an XML serializer that **fully supports the serialization programming model that was used by the `BinaryFormatter`**. It requires the known types to be specified up-front (but most .NET collections and primitive types are on a default allowlist and don't need to be specified). **It's the serializer that requires the least amount of effort to migrate to**.
* [System.Text.Json](https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/overview) is strict by default and avoids any guessing or interpretation on the caller's behalf, emphasizing deterministic behavior. The library is intentionally designed this way for performance and security. In contrary to `BinaryFormatter`, by default it serializes **only public properties**. To deserialize a `readonly` field or property, `[JsonConstructor]` attribute needs be used to indicate that given constructor should be used to set these. It supports serialization and deserialization of most built-in collections, but it does not support dictionaries where keys are not primitive types. It supports polymorphic type hierarchy serialization and deserialization, but it needs to be explicitly enabled. The same goes for preserving the references and handling circular references. The default behavior can be changed by writing custom converters.
* [MessagePack](https://github.com/MessagePack-CSharp/MessagePack-CSharp) provides a **highly efficient binary serialization format**, resulting in smaller message sizes compared to JSON and XML. It's **very performant** and ships with built-in support for LZ4 compression. It **supports only public types**, and it works best when all serializable types and members are annotated with dedicated attributes. It does not serialize non-public members by default, but it can be customized. It supports readonly types and members, by trying to select the best matching constructor. The constructor can be selected in an explicit way by using the `[SerializationConstructor]` attribute.
