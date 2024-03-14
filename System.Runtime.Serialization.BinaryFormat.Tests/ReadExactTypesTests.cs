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
    public class CustomTypeWithObjectField
    {
        public object? ActualObject;
        public object? SomeObject;
        public object? ReferenceToSameObject;
        public object? ReferenceToSelf;
    }

    [Fact]
    public void CanRead_CustomTypeWithObjectFields()
    {
        CustomTypeWithObjectField input = new()
        {
            ActualObject = new object(),
            SomeObject = "string"
        };

        input.ReferenceToSameObject = input.ActualObject;
        input.ReferenceToSelf = input;

        using MemoryStream stream = Serialize(input);

        ClassRecord serializationRecord = SafePayloadReader.ReadClassRecord<CustomTypeWithObjectField>(stream);

        Assert.Equal(input.SomeObject, serializationRecord[nameof(CustomTypeWithObjectField.SomeObject)]);
        Assert.Same(serializationRecord[nameof(CustomTypeWithObjectField.ActualObject)],
                    serializationRecord[nameof(CustomTypeWithObjectField.ReferenceToSameObject)]);
        Assert.Same(serializationRecord, serializationRecord[nameof(CustomTypeWithObjectField.ReferenceToSelf)]);
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

        ClassRecord classRecord = SafePayloadReader.ReadClassRecord<CustomTypeWithPrimitiveArrayFields>(stream);

        Verify(input.Bytes, classRecord, nameof(CustomTypeWithPrimitiveArrayFields.Bytes));
        Verify(input.SignedBytes, classRecord, nameof(CustomTypeWithPrimitiveArrayFields.SignedBytes));
        Verify(input.Shorts, classRecord, nameof(CustomTypeWithPrimitiveArrayFields.Shorts));
        Verify(input.UnsignedShorts, classRecord, nameof(CustomTypeWithPrimitiveArrayFields.UnsignedShorts));
        Verify(input.Integers, classRecord, nameof(CustomTypeWithPrimitiveArrayFields.Integers));
        Verify(input.UnsignedIntegers, classRecord, nameof(CustomTypeWithPrimitiveArrayFields.UnsignedIntegers));
        Verify(input.Longs, classRecord, nameof(CustomTypeWithPrimitiveArrayFields.Longs));
        Verify(input.UnsignedLongs, classRecord, nameof(CustomTypeWithPrimitiveArrayFields.UnsignedLongs));

        static void Verify<T>(T[] expected, ClassRecord classRecord, string fieldName) where T : unmanaged
        {
            var arrayRecord = (ArrayRecord<T>)classRecord[fieldName]!;
            Assert.Equal(expected, arrayRecord.ToArray());
        }
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
        ArrayRecord<string?> arrayRecord = (ArrayRecord<string?>)serializationRecord[nameof(CustomTypeWithStringArrayField.Texts)]!;

        Assert.Equal(input.Texts, arrayRecord.ToArray());
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
        ArrayRecord<string?> arrayRecord = (ArrayRecord<string?>)serializationRecord[nameof(CustomTypeWithStringArrayField.Texts)]!;

        Assert.Equal(input.Texts, arrayRecord.ToArray());
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

#if !NETFRAMEWORK // Half was introduced in 5.0
    [Fact]
    public void ReadArrayOfPrimitiveType_Throws_NotSupportedException_ForPrimitivesThatAreNotSerializable()
    {
        Half[] input = [Half.MinValue, Half.MaxValue];

        Assert.Throws<SerializationException>(() => Serialize(input));
        // we throw a different exception than BinaryFormatter
        Assert.Throws<NotSupportedException>(() => SafePayloadReader.ReadArrayOfPrimitiveType<Half>(Stream.Null));
    }
#endif

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

        ClassRecord?[] array = ((ArrayRecord<ClassRecord>)classRecord[nameof(CustomTypeWithArrayOfComplexTypes.Array)]!).ToArray();
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
        ArrayRecord<object?> arrayRecord = (ArrayRecord<object?>)classRecord[nameof(CustomTypeWithArrayOfObjects.Array)]!;
        object?[] values = arrayRecord.ToArray();

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
#if !NETFRAMEWORK
        Verify(nint.MaxValue);
        Verify(nuint.MaxValue);
#endif
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