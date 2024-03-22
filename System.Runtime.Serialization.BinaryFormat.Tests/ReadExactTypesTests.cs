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

        ClassRecord classRecord = PayloadReader.ReadExactClassRecord<CustomTypeWithPrimitiveFields>(stream);

        Verify(input, classRecord);

        Assert.Throws<InvalidOperationException>(() => classRecord.GetBoolean(nameof(CustomTypeWithPrimitiveFields.Byte)));
    }

    private static void Verify(CustomTypeWithPrimitiveFields expected, ClassRecord classRecord)
    {
        Assert.Equal(expected.Byte, classRecord.GetByte(nameof(CustomTypeWithPrimitiveFields.Byte)));
        Assert.Equal(expected.SignedByte, classRecord.GetSByte(nameof(CustomTypeWithPrimitiveFields.SignedByte)));
        Assert.Equal(expected.Short, classRecord.GetInt16(nameof(CustomTypeWithPrimitiveFields.Short)));
        Assert.Equal(expected.UnsignedShort, classRecord.GetUInt16(nameof(CustomTypeWithPrimitiveFields.UnsignedShort)));
        Assert.Equal(expected.Integer, classRecord.GetInt32(nameof(CustomTypeWithPrimitiveFields.Integer)));
        Assert.Equal(expected.UnsignedInteger, classRecord.GetUInt32(nameof(CustomTypeWithPrimitiveFields.UnsignedInteger)));
        Assert.Equal(expected.Long, classRecord.GetInt64(nameof(CustomTypeWithPrimitiveFields.Long)));
        Assert.Equal(expected.UnsignedLong, classRecord.GetUInt64(nameof(CustomTypeWithPrimitiveFields.UnsignedLong)));
    }

    [Serializable]
    public class CustomTypeWithStringField
    {
        public string? Text;
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Hello!")]
    public void CanRead_CustomTypeWithStringFields(string? text)
    {
        CustomTypeWithStringField input = new()
        {
            Text = text
        };

        using MemoryStream stream = Serialize(input);

        ClassRecord serializationRecord = PayloadReader.ReadExactClassRecord<CustomTypeWithStringField>(stream);

        Assert.Equal(input.Text, serializationRecord.GetString(nameof(CustomTypeWithStringField.Text)));
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

        ClassRecord classRecord = PayloadReader.ReadExactClassRecord<CustomTypeWithObjectField>(stream);

        Assert.Equal(input.SomeObject, classRecord.GetString(nameof(CustomTypeWithObjectField.SomeObject)));
        Assert.Same(classRecord.GetObject(nameof(CustomTypeWithObjectField.ActualObject)),
                    classRecord.GetObject(nameof(CustomTypeWithObjectField.ReferenceToSameObject)));
        Assert.Same(classRecord, classRecord.GetClassRecord(nameof(CustomTypeWithObjectField.ReferenceToSelf)));
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

        ClassRecord classRecord = PayloadReader.ReadExactClassRecord<CustomTypeWithPrimitiveArrayFields>(stream);

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
            var arrayRecord = (ArrayRecord<T>)classRecord.GetSerializationRecord(fieldName)!;
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

        ClassRecord classRecord = PayloadReader.ReadExactClassRecord<CustomTypeWithStringArrayField>(Serialize(input));

        Assert.Equal(input.Texts, classRecord.GetArrayOfStrings(nameof(CustomTypeWithStringArrayField.Texts)));
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

        ClassRecord classRecord = PayloadReader.ReadExactClassRecord<CustomTypeWithStringArrayField>(Serialize(input));

        Assert.Equal(input.Texts, classRecord.GetArrayOfStrings(nameof(CustomTypeWithStringArrayField.Texts)));
    }

    [Fact]
    public void CanRead_RawStringArrays()
    {
        string[] input = ["TopObject", "Is", "An", "Array", "Of", "Strings"];

        using MemoryStream stream = Serialize(input);

        string?[] output = PayloadReader.ReadArrayOfStrings(stream);

        Assert.Equal(input, output);
    }

    [Fact]
    public void CanReadArraysOfPrimitiveTypes()
    {
        ulong[] input = [0, 1, 2, 3, 4, ulong.MaxValue];

        using MemoryStream stream = Serialize(input);

        ulong[] output = PayloadReader.ReadArrayOfPrimitiveType<ulong>(stream);

        Assert.Equal(input, output);
    }

#if !NETFRAMEWORK // Half was introduced in 5.0
    [Fact]
    public void ReadArrayOfPrimitiveType_Throws_NotSupportedException_ForPrimitivesThatAreNotSerializable()
    {
        Half[] input = [Half.MinValue, Half.MaxValue];

        Assert.Throws<SerializationException>(() => Serialize(input));
        // we throw a different exception than BinaryFormatter
        Assert.Throws<NotSupportedException>(() => PayloadReader.ReadArrayOfPrimitiveType<Half>(Stream.Null));
    }
#endif

    [Fact]
    public void CanRead_ComplexSystemType()
    {
        Exception input = new("Hello, World!");

        ClassRecord classRecord = PayloadReader.ReadExactClassRecord<Exception>(Serialize(input));

        Assert.Equal(input.Message, classRecord.GetString(nameof(Exception.Message)));
    }

    [Fact]
    public void CanRead_ComplexSystemType_ThatReferencesOtherClassRecord()
    {
        ArgumentNullException inner = new(paramName: "innerPara");
        Exception outer = new("outer", inner);

        ClassRecord outerRecord = PayloadReader.ReadExactClassRecord<Exception>(Serialize(outer));

        Assert.Equal(outer.Message, outerRecord.GetString(nameof(Exception.Message)));

        ClassRecord innerRecord = outerRecord.GetClassRecord(nameof(Exception.InnerException))!;
        Assert.Equal(inner.ParamName, innerRecord.GetString(nameof(ArgumentNullException.ParamName)));
        Assert.Null(innerRecord.GetClassRecord(nameof(Exception.InnerException)));
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

        ClassRecord?[] classRecords = PayloadReader.ReadArrayOfExactClassRecords<CustomTypeWithPrimitiveFields>(stream);

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

        ClassRecord classRecord = PayloadReader.ReadExactClassRecord<CustomTypeWithArrayOfComplexTypes>(Serialize(input));

        ClassRecord?[] array = classRecord.GetArrayOfClassRecords(nameof(CustomTypeWithArrayOfComplexTypes.Array))!;
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

        ClassRecord classRecord = PayloadReader.ReadExactClassRecord<CustomTypeWithArrayOfComplexTypes>(stream);

        ClassRecord?[] array = classRecord.GetArrayOfClassRecords(nameof(CustomTypeWithArrayOfComplexTypes.Array))!;
        Assert.Equal(nullCount, array.Length);
        Assert.All(array, Assert.Null);

        Assert.Throws<SerializationException>(() => classRecord.GetArrayOfClassRecords(nameof(CustomTypeWithArrayOfComplexTypes.Array), allowNulls: false));
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

        object?[] output = PayloadReader.ReadArrayOfObjects(stream);

        Assert.Equal(input, output);
    }

    [Theory]
    [InlineData(byte.MaxValue)] // ObjectNullMultiple256
    [InlineData(byte.MaxValue + 2)] // ObjectNullMultiple
    public void CanRead_ArraysOfObjects_MultipleNulls(int nullCount)
    {
        object?[] input = Enumerable.Repeat<object>(null!, nullCount).ToArray();

        using MemoryStream stream = Serialize(input);

        object?[] output = PayloadReader.ReadArrayOfObjects(stream);

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

        ClassRecord classRecord = PayloadReader.ReadExactClassRecord<CustomTypeWithArrayOfObjects>(Serialize(input));

        Assert.Equal(input.Array, classRecord.GetArrayOfObjects(nameof(CustomTypeWithArrayOfObjects.Array)));
    }

    [Theory]
    [InlineData("notEmpty")]
    [InlineData("")] // null is prohibited by the BinaryFormatter itself
    public void CanReadString(string input)
    {
        using MemoryStream stream = Serialize(input);

        string output = PayloadReader.ReadString(stream);

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

            T output = PayloadReader.ReadPrimitiveType<T>(stream);

            Assert.Equal(input, output);
        }
    }

    [Serializable]
    public struct SerializableStruct
    {
        public int Integer;
        public string? Text;
    }

    [Fact]
    public void CanReadStruct()
    {
        SerializableStruct input = new()
        {
            Integer = 1988,
            Text = "StructsAreRepresentedWithClassRecords"
        };

        ClassRecord classRecord = PayloadReader.ReadExactClassRecord<SerializableStruct>(Serialize(input));

        Assert.Equal(input.Integer, classRecord.GetInt32(nameof(SerializableStruct.Integer)));
        Assert.Equal(input.Text, classRecord.GetString(nameof(SerializableStruct.Text)));
    }
}