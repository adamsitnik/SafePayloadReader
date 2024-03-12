using System.IO;
using System.Linq;
using Xunit;

namespace System.Runtime.Serialization.BinaryFormat.Tests;

public class JaggedArraysTests : ReadTests
{
    [Fact]
    public void CanReadJaggedArraysOfPrimitiveTypes()
    {
        int[][] array = new int[3][];
        for (int i = 0; i < array.Length; i++)
        {
            array[i] = [i, i, i];
        }

        using MemoryStream stream = Serialize(array);

        var arrayRecord = (ArrayRecord)SafePayloadReader.Read(stream);
        Assert.Equal((uint)array.Length, arrayRecord.Length);
    }

    [Fact]
    public void CanReadJaggedArraysOfStrings()
    {
        string[][] array = new string[3][];
        for (int i = 0; i < array.Length; i++)
        {
            array[i] = ["a", "b", "c"];
        }

        using MemoryStream stream = Serialize(array);

        var arrayRecord = (ArrayRecord)SafePayloadReader.Read(stream);
        Assert.Equal((uint)array.Length, arrayRecord.Length);
    }

    [Fact]
    public void CanReadJaggedArraysOfObjects()
    {
        object[][] array = new object[3][];
        for (int i = 0; i < array.Length; i++)
        {
            array[i] = ["a", 1, DateTime.MaxValue];
        }

        using MemoryStream stream = Serialize(array);

        var arrayRecord = (ArrayRecord)SafePayloadReader.Read(stream);
        Assert.Equal((uint)array.Length, arrayRecord.Length);
    }

    [Serializable]
    public class ComplexType
    {
        public int SomeField;
    }

    [Fact]
    public void CanReadJaggedArraysOfComplexTypes()
    {
        ComplexType[][] array = new ComplexType[3][];
        for (int i = 0; i < array.Length; i++)
        {
            array[i] = Enumerable.Range(0, i + 1).Select(j => new ComplexType { SomeField = j }).ToArray();
        }

        using MemoryStream stream = Serialize(array);

        var arrayRecord = (ArrayRecord)SafePayloadReader.Read(stream);
        Assert.Equal((uint)array.Length, arrayRecord.Length);
    }
}
