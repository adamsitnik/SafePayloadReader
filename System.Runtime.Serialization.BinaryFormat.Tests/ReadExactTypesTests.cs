using System.IO;
using System.Linq;
using Xunit;

namespace System.Runtime.Serialization.BinaryFormat.Tests;

public class ReadExactTypesTests : ReadTests
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

        ClassRecord serializationRecord = SafePayloadReader.ReadClassRecord<CustomTypeWithPrimitiveFields>(stream);

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

        ClassRecord serializationRecord = SafePayloadReader.ReadClassRecord<CustomTypeWithStringField>(stream);

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

        ClassRecord serializationRecord = SafePayloadReader.ReadClassRecord<CustomTypeWithPrimitiveArrayFields>(stream);

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

        ClassRecord serializationRecord = SafePayloadReader.ReadClassRecord<CustomTypeWithStringArrayField>(stream);

        Assert.Equal(input.Texts, serializationRecord[nameof(CustomTypeWithStringArrayField.Texts)]);
    }

    [Theory]
    [InlineData(byte.MaxValue)] // ObjectNullMultiple256
    [InlineData(byte.MaxValue + 2)] // ObjectNullMultiple
    public void CanRead_CustomTypeWithMultipleNullsInStringsArray(int nullCount)
    {
        CustomTypeWithStringArrayField input = new()
        {
            Texts = Enumerable.Repeat<string>(null!, nullCount).ToArray()
        };

        using MemoryStream stream = Serialize(input);

        ClassRecord serializationRecord = SafePayloadReader.ReadClassRecord<CustomTypeWithStringArrayField>(stream);

        Assert.Equal(input.Texts, serializationRecord[nameof(CustomTypeWithStringArrayField.Texts)]);
    }

    [Fact]
    public void CanRead_RawStringArrays()
    {
        string[] input = ["TopObject", "Is", "An", "Array", "Of", "Strings"];

        using MemoryStream stream = Serialize(input);

        string?[] output = SafePayloadReader.ReadArrayOfStrings(stream);

        Assert.Equal(input, output);
    }

    [Fact]
    public void CanReadArraysOfPrimitiveTypes()
    {
        ulong[] input = [0, 1, 2, 3, 4, ulong.MaxValue];

        using MemoryStream stream = Serialize(input);

        ulong[] output = SafePayloadReader.ReadArrayOfPrimitiveType<ulong>(stream);

        Assert.Equal(input, output);
    }

    [Fact]
    public void ReadArrayOfPrimitiveType_Throws_NotSupportedException_ForPrimitivesThatAreNotSerializable()
    {
        Half[] input = [Half.MinValue, Half.MaxValue];

        Assert.Throws<SerializationException>(() => Serialize(input));
        // we throw a different exception than BinaryFormatter
        Assert.Throws<NotSupportedException>(() => SafePayloadReader.ReadArrayOfPrimitiveType<Half>(Stream.Null));
    }

    [Fact]
    public void CanRead_ComplexSystemType()
    {
        Exception input = new("Hello, World!");

        using MemoryStream stream = Serialize(input);

        ClassRecord classRecord = SafePayloadReader.ReadClassRecord<Exception>(stream);

        Assert.Equal(input.Message, classRecord[nameof(Exception.Message)]);
    }

    [Fact]
    public void CanRead_ComplexSystemType_ThatReferencesOtherClassRecord()
    {
        ArgumentNullException inner = new(paramName: "innerPara");
        Exception outer = new("outer", inner);

        using MemoryStream stream = Serialize(outer);

        ClassRecord outerRecord = SafePayloadReader.ReadClassRecord<Exception>(stream);

        Assert.Equal(outer.Message, outerRecord[nameof(Exception.Message)]);

        ClassRecord innerRecord = (ClassRecord)outerRecord[nameof(Exception.InnerException)]!;
        Assert.Equal(inner.ParamName, innerRecord[nameof(ArgumentNullException.ParamName)]);
        Assert.Null(innerRecord[nameof(Exception.InnerException)]);
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

        ClassRecord?[] classRecords = SafePayloadReader.ReadArrayOfClassRecords(stream);

        for (int i = 0; i < input.Length; i++)
        {
            Verify(input[i], classRecords[i]!);
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

        ClassRecord classRecord = SafePayloadReader.ReadClassRecord<CustomTypeWithArrayOfComplexTypes>(stream);

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

        ClassRecord classRecord = SafePayloadReader.ReadClassRecord<CustomTypeWithArrayOfComplexTypes>(stream);

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

        object?[] output = SafePayloadReader.ReadArrayOfObjects(stream);

        Assert.Equal(input, output);
    }

    [Theory]
    [InlineData(byte.MaxValue)] // ObjectNullMultiple256
    [InlineData(byte.MaxValue + 2)] // ObjectNullMultiple
    public void CanRead_ArraysOfObjects_MultipleNulls(int nullCount)
    {
        object?[] input = Enumerable.Repeat<object>(null!, nullCount).ToArray();

        using MemoryStream stream = Serialize(input);

        object?[] output = SafePayloadReader.ReadArrayOfObjects(stream);

        Assert.Equal(nullCount, output.Length);
        Assert.All(output, Assert.Null);
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

        ClassRecord classRecord = SafePayloadReader.ReadClassRecord<CustomTypeWithArrayOfObjects>(stream);

        object[] values = (object[])classRecord[nameof(CustomTypeWithArrayOfObjects.Array)]!;

        Assert.Equal(input.Array, values);
    }

    [Theory]
    [InlineData("notEmpty")]
    [InlineData("")] // null is prohibited by the BinaryFormatter itself
    public void CanReadString(string input)
    {
        using MemoryStream stream = Serialize(input);

        string output = SafePayloadReader.ReadString(stream);

        Assert.Equal(input, output);
    }

    [Fact]
    public void CanReadPrimitiveTypes()
    {
        Verify(true);
        Verify('c');
        Verify(byte.MaxValue);
        Verify(sbyte.MaxValue);
        Verify(short.MaxValue);
        Verify(ushort.MaxValue);
        Verify(int.MaxValue);
        Verify(uint.MaxValue);
        Verify(nint.MaxValue);
        Verify(nuint.MaxValue);
        Verify(long.MaxValue);
        Verify(ulong.MaxValue);
        Verify(float.MaxValue);
        Verify(double.MaxValue);
        Verify(decimal.MaxValue);
        Verify(TimeSpan.MaxValue);
        Verify(DateTime.Now);

        static void Verify<T>(T input) where T : unmanaged
        {
            using MemoryStream stream = Serialize(input);

            T output = SafePayloadReader.ReadPrimitiveType<T>(stream);

            Assert.Equal(input, output);
        }
    }
}