using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using Xunit;

namespace System.Runtime.Serialization.BinaryFormat.Tests;

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

[Serializable]
public class CustomTypeWithStringField
{
    public string? Text;
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

[Serializable]
public class CustomTypeWithStringArrayField
{
    public string[]? Texts;
}

public class ReadScenarioTests
{
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

        ClassWithMembersAndTypesRecord serializationRecord = (ClassWithMembersAndTypesRecord)payload.GetTopLevel<CustomTypeWithPrimitiveFields>();

        Assert.Equal(input.Byte, serializationRecord[nameof(CustomTypeWithPrimitiveFields.Byte)]);
        Assert.Equal(input.SignedByte, serializationRecord[nameof(CustomTypeWithPrimitiveFields.SignedByte)]);
        Assert.Equal(input.Short, serializationRecord[nameof(CustomTypeWithPrimitiveFields.Short)]);
        Assert.Equal(input.UnsignedShort, serializationRecord[nameof(CustomTypeWithPrimitiveFields.UnsignedShort)]);
        Assert.Equal(input.Integer, serializationRecord[nameof(CustomTypeWithPrimitiveFields.Integer)]);
        Assert.Equal(input.UnsignedInteger, serializationRecord[nameof(CustomTypeWithPrimitiveFields.UnsignedInteger)]);
        Assert.Equal(input.Long, serializationRecord[nameof(CustomTypeWithPrimitiveFields.Long)]);
        Assert.Equal(input.UnsignedLong, serializationRecord[nameof(CustomTypeWithPrimitiveFields.UnsignedLong)]);
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

        ClassWithMembersAndTypesRecord serializationRecord = (ClassWithMembersAndTypesRecord)payload.GetTopLevel<CustomTypeWithStringField>();

        Assert.Equal(input.Text, serializationRecord[nameof(CustomTypeWithStringField.Text)]);
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

        ClassWithMembersAndTypesRecord serializationRecord = (ClassWithMembersAndTypesRecord)payload.GetTopLevel<CustomTypeWithPrimitiveArrayFields>();

        Assert.Equal(input.Bytes, serializationRecord[nameof(CustomTypeWithPrimitiveArrayFields.Bytes)]);
        Assert.Equal(input.SignedBytes, serializationRecord[nameof(CustomTypeWithPrimitiveArrayFields.SignedBytes)]);
        Assert.Equal(input.Shorts, serializationRecord[nameof(CustomTypeWithPrimitiveArrayFields.Shorts)]);
        Assert.Equal(input.UnsignedShorts, serializationRecord[nameof(CustomTypeWithPrimitiveArrayFields.UnsignedShorts)]);
        Assert.Equal(input.Integers, serializationRecord[nameof(CustomTypeWithPrimitiveArrayFields.Integers)]);
        Assert.Equal(input.UnsignedIntegers, serializationRecord[nameof(CustomTypeWithPrimitiveArrayFields.UnsignedIntegers)]);
        Assert.Equal(input.Longs, serializationRecord[nameof(CustomTypeWithPrimitiveArrayFields.Longs)]);
        Assert.Equal(input.UnsignedLongs, serializationRecord[nameof(CustomTypeWithPrimitiveArrayFields.UnsignedLongs)]);
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

        ClassWithMembersAndTypesRecord serializationRecord = (ClassWithMembersAndTypesRecord)payload.GetTopLevel<CustomTypeWithStringArrayField>();

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

        ClassWithMembersAndTypesRecord serializationRecord = (ClassWithMembersAndTypesRecord)payload.GetTopLevel<CustomTypeWithStringArrayField>();

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

    private static MemoryStream Serialize<T>(T instance)
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