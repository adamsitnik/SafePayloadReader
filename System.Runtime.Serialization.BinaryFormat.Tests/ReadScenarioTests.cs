using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using Xunit;

namespace System.Runtime.Serialization.BinaryFormat.Tests;

public class ReadScenarioTests
{
    [Serializable]
    public class CustomTypeWithPrimitiveFields
    {
        public byte Byte;
        public sbyte SignedByte;
        public short Short;
        public ushort UnsignedShort;
        public int Integer;
        public uint UnsignedInteger;
        public long Long;
        public ulong UnsignedLong;
    }

    [Fact]
    public void CanRead_CustomTypeWithPrimitiveFields()
    {
        CustomTypeWithPrimitiveFields input = new()
        {
            Byte = 1,
            SignedByte = 2,
            Short = -3,
            UnsignedShort = 4,
            Integer = -123,
            UnsignedInteger = 666,
            Long = long.MaxValue,
            UnsignedLong = ulong.MaxValue
        };

        using MemoryStream stream = Serialize(input);

        var payload = SafePayloadReader.Read(stream);

        ClassRecord serializationRecord = (ClassRecord)payload.GetTopLevel<CustomTypeWithPrimitiveFields>();

        Verify(input, serializationRecord);
    }

    private static void Verify(CustomTypeWithPrimitiveFields expected, ClassRecord classRecord)
    {
        Assert.Equal(expected.Byte, classRecord[nameof(CustomTypeWithPrimitiveFields.Byte)]);
        Assert.Equal(expected.SignedByte, classRecord[nameof(CustomTypeWithPrimitiveFields.SignedByte)]);
        Assert.Equal(expected.Short, classRecord[nameof(CustomTypeWithPrimitiveFields.Short)]);
        Assert.Equal(expected.UnsignedShort, classRecord[nameof(CustomTypeWithPrimitiveFields.UnsignedShort)]);
        Assert.Equal(expected.Integer, classRecord[nameof(CustomTypeWithPrimitiveFields.Integer)]);
        Assert.Equal(expected.UnsignedInteger, classRecord[nameof(CustomTypeWithPrimitiveFields.UnsignedInteger)]);
        Assert.Equal(expected.Long, classRecord[nameof(CustomTypeWithPrimitiveFields.Long)]);
        Assert.Equal(expected.UnsignedLong, classRecord[nameof(CustomTypeWithPrimitiveFields.UnsignedLong)]);
    }

    [Serializable]
    public class CustomTypeWithStringField
    {
        public string? Text;
    }

    [Fact]
    public void CanRead_CustomTypeWithStringFields()
    {
        CustomTypeWithStringField input = new()
        {
            Text = "Hello!"
        };

        using MemoryStream stream = Serialize(input);

        var payload = SafePayloadReader.Read(stream);

        ClassRecord serializationRecord = (ClassRecord)payload.GetTopLevel<CustomTypeWithStringField>();

        Assert.Equal(input.Text, serializationRecord[nameof(CustomTypeWithStringField.Text)]);
    }

    [Serializable]
    public class CustomTypeWithPrimitiveArrayFields
    {
        public byte[]? Bytes;
        public sbyte[]? SignedBytes;
        public short[]? Shorts;
        public ushort[]? UnsignedShorts;
        public int[]? Integers;
        public uint[]? UnsignedIntegers;
        public long[]? Longs;
        public ulong[]? UnsignedLongs;
    }

    [Fact]
    public void CanRead_CustomTypeWithPrimitiveArrayFields()
    {
        CustomTypeWithPrimitiveArrayFields input = new()
        {
            Bytes = [1, 2],
            SignedBytes = [2, 3, 4],
            Shorts = [-3, 3],
            UnsignedShorts = [4, 45],
            Integers = [-123, 222],
            UnsignedIntegers = [666, 300, 7],
            Longs = [long.MaxValue, long.MinValue],
            UnsignedLongs = [ulong.MaxValue, ulong.MinValue],
        };

        using MemoryStream stream = Serialize(input);

        var payload = SafePayloadReader.Read(stream);

        ClassRecord serializationRecord = (ClassRecord)payload.GetTopLevel<CustomTypeWithPrimitiveArrayFields>();

        Assert.Equal(input.Bytes, serializationRecord[nameof(CustomTypeWithPrimitiveArrayFields.Bytes)]);
        Assert.Equal(input.SignedBytes, serializationRecord[nameof(CustomTypeWithPrimitiveArrayFields.SignedBytes)]);
        Assert.Equal(input.Shorts, serializationRecord[nameof(CustomTypeWithPrimitiveArrayFields.Shorts)]);
        Assert.Equal(input.UnsignedShorts, serializationRecord[nameof(CustomTypeWithPrimitiveArrayFields.UnsignedShorts)]);
        Assert.Equal(input.Integers, serializationRecord[nameof(CustomTypeWithPrimitiveArrayFields.Integers)]);
        Assert.Equal(input.UnsignedIntegers, serializationRecord[nameof(CustomTypeWithPrimitiveArrayFields.UnsignedIntegers)]);
        Assert.Equal(input.Longs, serializationRecord[nameof(CustomTypeWithPrimitiveArrayFields.Longs)]);
        Assert.Equal(input.UnsignedLongs, serializationRecord[nameof(CustomTypeWithPrimitiveArrayFields.UnsignedLongs)]);
    }

    [Serializable]
    public class CustomTypeWithStringArrayField
    {
        public string[]? Texts;
    }

    [Theory]
    [InlineData("Hello", ", ", "World!")]
    [InlineData("Single ", "null", null)]
    [InlineData("Multiple ", null, null)]
    public void CanRead_CustomTypeWithStringsArrayField(string value0, string value1, string value2)
    {
        CustomTypeWithStringArrayField input = new()
        {
            Texts = [value0, value1, value2]
        };

        using MemoryStream stream = Serialize(input);

        var payload = SafePayloadReader.Read(stream);

        ClassRecord serializationRecord = (ClassRecord)payload.GetTopLevel<CustomTypeWithStringArrayField>();

        Assert.Equal(input.Texts, serializationRecord[nameof(CustomTypeWithStringArrayField.Texts)]);
    }

    [Fact]
    public void CanRead_CustomTypeWithMultipleNullsInStringsArray()
    {
        CustomTypeWithStringArrayField input = new()
        {
            Texts = Enumerable.Repeat<string>(null!, byte.MaxValue + 1).ToArray()
        };

        using MemoryStream stream = Serialize(input);

        var payload = SafePayloadReader.Read(stream);

        ClassRecord serializationRecord = (ClassRecord)payload.GetTopLevel<CustomTypeWithStringArrayField>();

        Assert.Equal(input.Texts, serializationRecord[nameof(CustomTypeWithStringArrayField.Texts)]);
    }

    [Fact]
    public void CanRead_RawStringArrays()
    {
        string[] input = ["TopObject", "Is", "An", "Array", "Of", "Strings"];

        using MemoryStream stream = Serialize(input);

        var payload = SafePayloadReader.Read(stream);

        SerializationRecord topLevel = payload.GetTopLevel<string[]>();

        Assert.Equal(input, topLevel.GetValue());
    }

    [Fact]
    public void CanRead_RawArraysOfPrimitiveTypes()
    {
        ulong[] input = [0, 1, 2, 3, 4, ulong.MaxValue];

        using MemoryStream stream = Serialize(input);

        var payload = SafePayloadReader.Read(stream);

        SerializationRecord topLevel = payload.GetTopLevel<ulong[]>();

        Assert.Equal(input, topLevel.GetValue());
    }

    [Fact]
    public void CanRead_ComplexSystemType()
    {
        Exception input = new("Hello, World!");

        using MemoryStream stream = Serialize(input);

        var payload = SafePayloadReader.Read(stream);

        ClassRecord classRecord = (ClassRecord)payload.GetTopLevel<Exception>();

        Assert.Equal(input.Message, classRecord[nameof(Exception.Message)]);
    }

    [Fact]
    public void CanRead_ArraysOfComplexTypes()
    {
        CustomTypeWithPrimitiveFields[] input = [
            new () { Byte = 1 },
            new () { Integer = 3 },
            new () { Short = 4 },
            new () { Long = 5 },
        ];

        using MemoryStream stream = Serialize(input);

        var payload = SafePayloadReader.Read(stream);

        SerializationRecord topLevel = payload.GetTopLevel<CustomTypeWithPrimitiveFields[]>();

        object[] array = (object[])topLevel.GetValue();

        for (int i = 0; i < input.Length; i++)
        {
            Verify(input[i], (ClassRecord)array[i]);
        }
    }

    [Serializable]
    public class CustomTypeWithArrayOfComplexTypes
    {
        public CustomTypeWithPrimitiveFields?[]? Array;
    }

    [Fact]
    public void CanRead_TypesWithArraysOfComplexTypes()
    {
        CustomTypeWithArrayOfComplexTypes input = new()
        {
            Array =
            [
                new() { Byte = 1 },
                new() { Integer = 2 },
                new() { Short = 3 },
                new() { Long = 4 },
                new() { UnsignedInteger = 5 },
                null!
            ]
        };

        using MemoryStream stream = Serialize(input);

        var payload = SafePayloadReader.Read(stream);

        ClassRecord classRecord = (ClassRecord)payload.GetTopLevel<CustomTypeWithArrayOfComplexTypes>();

        object array = classRecord[nameof(CustomTypeWithArrayOfComplexTypes.Array)]!;
    }

    [Theory]
    [InlineData(byte.MaxValue)] // ObjectNullMultiple256
    [InlineData(byte.MaxValue + 2)] // ObjectNullMultiple
    public void CanRead_TypesWithArraysOfComplexTypes_MultipleNulls(int nullCount)
    {
        CustomTypeWithArrayOfComplexTypes input = new()
        {
            Array = Enumerable.Repeat<CustomTypeWithPrimitiveFields>(null!, nullCount).ToArray()
        };

        using MemoryStream stream = Serialize(input);

        var payload = SafePayloadReader.Read(stream);

        ClassRecord classRecord = (ClassRecord)payload.GetTopLevel<CustomTypeWithArrayOfComplexTypes>();

        object[] array = (object[])classRecord[nameof(CustomTypeWithArrayOfComplexTypes.Array)]!;
        Assert.Equal(nullCount, array.Length);
        Assert.All(array, Assert.Null);
    }

    [Fact]
    public void CanRead_ArraysOfObjects()
    {
        object?[] input = [
            1,
            "test",
            null
        ];

        using MemoryStream stream = Serialize(input);

        var payload = SafePayloadReader.Read(stream);

        SerializationRecord serializationRecord = payload.GetTopLevel<object[]>();

        object[] value = (object[])serializationRecord.GetValue()!;

        Assert.Equal(input, value);
    }

    [Theory]
    [InlineData(byte.MaxValue)] // ObjectNullMultiple256
    [InlineData(byte.MaxValue + 2)] // ObjectNullMultiple
    public void CanRead_ArraysOfObjects_MultipleNulls(int nullCount)
    {
        object?[] input = Enumerable.Repeat<object>(null!, nullCount).ToArray();

        using MemoryStream stream = Serialize(input);

        var payload = SafePayloadReader.Read(stream);

        SerializationRecord serializationRecord = payload.GetTopLevel<object[]>();

        object[] array = (object[])serializationRecord.GetValue();
        Assert.Equal(nullCount, array.Length);
        Assert.All(array, Assert.Null);
    }

    [Serializable]
    public class CustomTypeWithArrayOfObjects
    {
        public object?[]? Array;
    }

    [Fact]
    public void CanRead_CustomTypeWithArrayOfObjects()
    {
        CustomTypeWithArrayOfObjects input = new()
        {
            Array = [
                1,
                false,
                "string",
                null
            ]
        };

        using MemoryStream stream = Serialize(input);

        var payload = SafePayloadReader.Read(stream);

        ClassRecord classRecord = (ClassRecord)payload.GetTopLevel<CustomTypeWithArrayOfObjects>();

        object[] values = (object[])classRecord[nameof(CustomTypeWithArrayOfObjects.Array)]!;

        Assert.Equal(input.Array, values);
    }

    private static MemoryStream Serialize<T>(T instance) where T : notnull
    {
        MemoryStream ms = new();
#pragma warning disable SYSLIB0011 // Type or member is obsolete
        BinaryFormatter binaryFormatter = new();
#pragma warning restore SYSLIB0011 // Type or member is obsolete
        binaryFormatter.Serialize(ms, instance);

        ms.Position = 0;
        return ms;
    }
}