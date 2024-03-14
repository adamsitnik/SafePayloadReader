using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Runtime.Serialization.BinaryFormat.Tests;

// TODO: add test cases for generic types, for both system and non-system
// This should exercise type forwards code path.
public class TypeMatchTests : ReadTests
{
    private readonly static HashSet<Type> PrimitiveTypes = new()
    {
        typeof(bool), typeof(char), typeof(byte), typeof(sbyte),
        typeof(short), typeof(ushort), typeof(int), typeof(uint),
        typeof(long), typeof(ulong), typeof(IntPtr), typeof(UIntPtr),
        typeof(float), typeof(double), typeof(decimal), typeof(DateTime),
        typeof(TimeSpan)
    };

    [Serializable]
    public class NonSystemClass
    { 
    }

    [Fact]
    public void CanRecognizeAllSupportedPrimitiveTypes()
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
    }

    [Fact]
    public void CanRecognizeSystemTypes()
    {
        Verify(new NotSupportedException());
    }

    [Fact]
    public void CanRecognizeNonSystemTypes()
    {
        Verify(new NonSystemClass());
    }

    [Fact]
    public void CanRecognizeSZArraysOfAllSupportedPrimitiveTypes()
    {
        VerifySZArray(true);
        VerifySZArray('c');
        VerifySZArray(byte.MaxValue);
        VerifySZArray(sbyte.MaxValue);
        VerifySZArray(short.MaxValue);
        VerifySZArray(ushort.MaxValue);
        VerifySZArray(int.MaxValue);
        VerifySZArray(uint.MaxValue);
#if !NETFRAMEWORK
        VerifySZArray(nint.MaxValue);
        VerifySZArray(nuint.MaxValue);
#endif
        VerifySZArray(long.MaxValue);
        VerifySZArray(ulong.MaxValue);
        VerifySZArray(float.MaxValue);
        VerifySZArray(double.MaxValue);
        VerifySZArray(decimal.MaxValue);
        VerifySZArray(TimeSpan.MaxValue);
        VerifySZArray(DateTime.Now);
    }

    [Fact]
    public void CanRecognizeSZArraysOfSystemTypes()
    {
        VerifySZArray(new NotSupportedException());
    }

    [Fact]
    public void CanRecognizeSZArraysOfNonSystemTypes()
    {
        VerifySZArray(new NonSystemClass());
    }

    [Fact]
    public void CanRecognizeJaggedArraysOfAllSupportedPrimitiveTypes()
    {
        VerifyJaggedArray(true);
        VerifyJaggedArray('c');
        VerifyJaggedArray(byte.MaxValue);
        VerifyJaggedArray(sbyte.MaxValue);
        VerifyJaggedArray(short.MaxValue);
        VerifyJaggedArray(ushort.MaxValue);
        VerifyJaggedArray(int.MaxValue);
        VerifyJaggedArray(uint.MaxValue);
#if !NETFRAMEWORK
        VerifyJaggedArray(nint.MaxValue);
        VerifyJaggedArray(nuint.MaxValue);
#endif
        VerifyJaggedArray(long.MaxValue);
        VerifyJaggedArray(ulong.MaxValue);
        VerifyJaggedArray(float.MaxValue);
        VerifyJaggedArray(double.MaxValue);
        VerifyJaggedArray(decimal.MaxValue);
        VerifyJaggedArray(TimeSpan.MaxValue);
        VerifyJaggedArray(DateTime.Now);
    }

    [Fact]
    public void CanRecognizeJaggedArraysOfSystemTypes()
    {
        VerifyJaggedArray(new NotSupportedException());
    }

    [Fact]
    public void CanRecognizeJaggedArraysOfNonSystemTypes()
    {
        VerifyJaggedArray(new NonSystemClass());
    }

    [Fact]
    public void CanRecognizeRectangular2DArraysOfAllSupportedPrimitiveTypes()
    {
        VerifyRectangularArray_2D(true);
        VerifyRectangularArray_2D('c');
        VerifyRectangularArray_2D(byte.MaxValue);
        VerifyRectangularArray_2D(sbyte.MaxValue);
        VerifyRectangularArray_2D(short.MaxValue);
        VerifyRectangularArray_2D(ushort.MaxValue);
        VerifyRectangularArray_2D(int.MaxValue);
        VerifyRectangularArray_2D(uint.MaxValue);
#if !NETFRAMEWORK
        VerifyRectangularArray_2D(nint.MaxValue);
        VerifyRectangularArray_2D(nuint.MaxValue);
#endif
        VerifyRectangularArray_2D(long.MaxValue);
        VerifyRectangularArray_2D(ulong.MaxValue);
        VerifyRectangularArray_2D(float.MaxValue);
        VerifyRectangularArray_2D(double.MaxValue);
        VerifyRectangularArray_2D(decimal.MaxValue);
        VerifyRectangularArray_2D(TimeSpan.MaxValue);
        VerifyRectangularArray_2D(DateTime.Now);
    }

    [Fact]
    public void CanRecognizeRectangular2DArraysOfSystemTypes()
    {
        VerifyRectangularArray_2D(new NotSupportedException());
    }

    [Fact]
    public void CanRecognizeRectangular2DArraysNonOfSystemTypes()
    {
        VerifyRectangularArray_2D(new NonSystemClass());
    }

    [Fact]
    public void CanRecognizeRectangular5DArraysOfAllSupportedPrimitiveTypes()
    {
        VerifyRectangularArray_5D(true);
        VerifyRectangularArray_5D('c');
        VerifyRectangularArray_5D(byte.MaxValue);
        VerifyRectangularArray_5D(sbyte.MaxValue);
        VerifyRectangularArray_5D(short.MaxValue);
        VerifyRectangularArray_5D(ushort.MaxValue);
        VerifyRectangularArray_5D(int.MaxValue);
        VerifyRectangularArray_5D(uint.MaxValue);
#if !NETFRAMEWORK
        VerifyRectangularArray_5D(nint.MaxValue);
        VerifyRectangularArray_5D(nuint.MaxValue);
#endif
        VerifyRectangularArray_5D(long.MaxValue);
        VerifyRectangularArray_5D(ulong.MaxValue);
        VerifyRectangularArray_5D(float.MaxValue);
        VerifyRectangularArray_5D(double.MaxValue);
        VerifyRectangularArray_5D(decimal.MaxValue);
        VerifyRectangularArray_5D(TimeSpan.MaxValue);
        VerifyRectangularArray_5D(DateTime.Now);
    }

    [Fact]
    public void CanRecognizeRectangular5DArraysOfSystemTypes()
    {
        VerifyRectangularArray_5D(new NotSupportedException());
    }

    [Fact]
    public void CanRecognizeRectangular5DArraysOfNonSystemTypes()
    {
        VerifyRectangularArray_5D(new NonSystemClass());
    }

    [Theory]
    [InlineData(1)] // ArrayType.SingleOffset
    [InlineData(2)] // ArrayType.JaggedOffset
    [InlineData(3)] // ArrayType.RectangularOffset
    [InlineData(32)] // max rank
    public void CanRecognizeArraysOfAllSupportedPrimitiveTypesWithCustomOffsets(int arrayRank)
    {
        VerifyCustomOffsetArray(true, arrayRank);
        VerifyCustomOffsetArray('c', arrayRank);
        VerifyCustomOffsetArray(byte.MaxValue, arrayRank);
        VerifyCustomOffsetArray(sbyte.MaxValue, arrayRank);
        VerifyCustomOffsetArray(short.MaxValue, arrayRank);
        VerifyCustomOffsetArray(ushort.MaxValue, arrayRank);
        VerifyCustomOffsetArray(int.MaxValue, arrayRank);
        VerifyCustomOffsetArray(uint.MaxValue, arrayRank);
#if !NETFRAMEWORK
        VerifyCustomOffsetArray(nint.MaxValue, arrayRank);
        VerifyCustomOffsetArray(nuint.MaxValue, arrayRank);
#endif
        VerifyCustomOffsetArray(long.MaxValue, arrayRank);
        VerifyCustomOffsetArray(ulong.MaxValue, arrayRank);
        VerifyCustomOffsetArray(float.MaxValue, arrayRank);
        VerifyCustomOffsetArray(double.MaxValue, arrayRank);
        VerifyCustomOffsetArray(decimal.MaxValue, arrayRank);
        VerifyCustomOffsetArray(TimeSpan.MaxValue, arrayRank);
        VerifyCustomOffsetArray(DateTime.Now, arrayRank);
    }

    [Theory]
    // [InlineData(1)] // ArrayType.SingleOffset bug in BinaryFormatter!!
    [InlineData(2)] // ArrayType.JaggedOffset
    [InlineData(3)] // ArrayType.RectangularOffset
    [InlineData(32)] // max rank
    public void CanRecognizeArraysOfSystemTypesWithCustomOffsets(int arrayRank)
    {
        VerifyCustomOffsetArray(new NotSupportedException(), arrayRank);
    }

    [Theory]
    // [InlineData(1)] // ArrayType.SingleOffset bug in BinaryFormatter!!
    [InlineData(2)] // ArrayType.JaggedOffset
    [InlineData(3)] // ArrayType.RectangularOffset
    [InlineData(32)] // max rank
    public void CanRecognizeArraysOfNonSystemTypesWithCustomOffsets(int arrayRank)
    {
        VerifyCustomOffsetArray(new NonSystemClass(), arrayRank);
    }

    private static void Verify<T>(T input) where T : notnull
    {
        SerializationRecord one = SafePayloadReader.Read(Serialize(input));

        foreach (Type type in PrimitiveTypes)
        {
            Assert.Equal(typeof(T) == type, one.IsSerializedInstanceOf(type));
        }
    }

    private static void VerifySZArray<T>(T input) where T : notnull
    {
        T[] array = [input];

        ArrayRecord arrayRecord = (ArrayRecord)SafePayloadReader.Read(Serialize(array));

        if (PrimitiveTypes.Contains(typeof(T)))
        {
            Assert.True(arrayRecord is ArrayRecord<T>, userMessage: typeof(T).Name);
        }
        else
        {
            Assert.True(arrayRecord is ArrayRecord<ClassRecord>, userMessage: typeof(T).Name);
            Assert.True(arrayRecord.IsSerializedInstanceOf(typeof(T[])));
        }

        foreach (Type type in PrimitiveTypes)
        {
            Assert.False(arrayRecord.IsSerializedInstanceOf(type));
            Assert.Equal(typeof(T) == type, arrayRecord.IsSerializedInstanceOf(type.MakeArrayType()));
        }

        if (PrimitiveTypes.Contains(typeof(T)))
        {
            Assert.Equal(array, arrayRecord.ToArray(typeof(T[])));
        }
    }

    private static void VerifyJaggedArray<T>(T input) where T : notnull
    {
        T[][] jaggedArray = [[input]];

        SerializationRecord arrayRecord = SafePayloadReader.Read(Serialize(jaggedArray));

        Assert.True(arrayRecord is ArrayRecord);

        if (PrimitiveTypes.Contains(typeof(T)))
        {
            Assert.True(arrayRecord is JaggedArrayRecord<T>, userMessage: typeof(T).Name);
        }
        else
        {
            Assert.True(arrayRecord is JaggedArrayRecord<ClassRecord>, userMessage: typeof(T).Name);
            Assert.False(arrayRecord.IsSerializedInstanceOf(typeof(T[])));
            Assert.True(arrayRecord.IsSerializedInstanceOf(typeof(T[][])));
            Assert.False(arrayRecord.IsSerializedInstanceOf(typeof(T[][][])));
        }

        foreach (Type type in PrimitiveTypes)
        {
            Assert.False(arrayRecord.IsSerializedInstanceOf(type));
            Assert.Equal(typeof(T) == type, arrayRecord.IsSerializedInstanceOf(type.MakeArrayType()));
            Assert.Equal(typeof(T) == type, arrayRecord.IsSerializedInstanceOf(type.MakeArrayType().MakeArrayType()));
            Assert.False(arrayRecord.IsSerializedInstanceOf(type.MakeArrayType().MakeArrayType().MakeArrayType()));
        }
    }

    private static void VerifyRectangularArray_2D<T>(T input) where T : notnull
    {
        T[,] rectangularArray = new T[1, 1];
        rectangularArray[0, 0] = input;

        VerifyRectangularArray<T>(rectangularArray);
    }

    private static void VerifyRectangularArray_5D<T>(T input) where T : notnull
    {
        T[,,,,] rectangularArray = new T[1, 1, 1, 1, 1];
        rectangularArray[0, 0, 0, 0, 0] = input;

        VerifyRectangularArray<T>(rectangularArray);
    }

    private static void VerifyRectangularArray<T>(Array array)
    {
        int arrayRank = array.GetType().GetArrayRank();
        SerializationRecord arrayRecord = SafePayloadReader.Read(Serialize(array));

        Assert.True(arrayRecord is ArrayRecord);
        Assert.False(arrayRecord is ArrayRecord<T>, userMessage: typeof(T).Name);
        Assert.False(arrayRecord is JaggedArrayRecord<T>, userMessage: typeof(T).Name);

        foreach (Type type in PrimitiveTypes.Concat([typeof(T)]))
        {
            Assert.False(arrayRecord.IsSerializedInstanceOf(type));
            Assert.False(arrayRecord.IsSerializedInstanceOf(type.MakeArrayType(arrayRank - 1)));
            Assert.Equal(typeof(T) == type, arrayRecord.IsSerializedInstanceOf(type.MakeArrayType(arrayRank)));
            Assert.False(arrayRecord.IsSerializedInstanceOf(type.MakeArrayType(arrayRank + 1)));
        }
    }

    private static void VerifyCustomOffsetArray<T>(T input, int arrayRank) where T : notnull
    {
        int[] lengths = Enumerable.Repeat(1  /* length */, arrayRank).ToArray();
        int[] offsets = Enumerable.Repeat(10 /* offset */, arrayRank).ToArray();

        Array array = Array.CreateInstance(typeof(T), lengths, offsets);
        for (int dimension = 0; dimension < lengths.Length; dimension++)
        {
            Assert.Equal(offsets[dimension], array.GetLowerBound(dimension));
        }
        array.SetValue(input, offsets);

        SerializationRecord arrayRecord = SafePayloadReader.Read(Serialize(array));

        Assert.True(arrayRecord is ArrayRecord);
        Assert.False(arrayRecord is ArrayRecord<T>, userMessage: typeof(T).Name);
        Assert.False(arrayRecord is JaggedArrayRecord<T>, userMessage: typeof(T).Name);

        foreach (Type type in PrimitiveTypes.Concat([typeof(T)]))
        {
            Assert.False(arrayRecord.IsSerializedInstanceOf(type));
            if (arrayRank > 1)
            {
                Assert.False(arrayRecord.IsSerializedInstanceOf(type.MakeArrayType(arrayRank - 1)));
            }
            Assert.Equal(typeof(T) == type, arrayRecord.IsSerializedInstanceOf(type.MakeArrayType(arrayRank)));
            if (arrayRank <= 31) // 32 is max
            {
                Assert.False(arrayRecord.IsSerializedInstanceOf(type.MakeArrayType(arrayRank + 1)));
            }
        }
    }
}
