using System.IO;
using System.Linq;
using Xunit;

namespace System.Runtime.Serialization.BinaryFormat.Tests;

public class JaggedArraysTests : ReadTests
{
    [Fact]
    public void CanReadJaggedArraysOfPrimitiveTypes()
    {
        int[][] input = new int[7][];
        for (int i = 0; i < input.Length; i++)
        {
            input[i] = [i, i, i];
        }

        var arrayRecord = (JaggedArrayRecord<int>)SafePayloadReader.Read(Serialize(input));

        Assert.Equal((uint)input.Length, arrayRecord.Length);
        Assert.Equal(input, arrayRecord.ToArray());
        Assert.Equal(input, arrayRecord.ToArray(input.GetType()));
    }

    [Fact]
    public void CanReadJaggedArraysOfStrings()
    {
        string[][] input = new string[5][];
        for (int i = 0; i < input.Length; i++)
        {
            input[i] = ["a", "b", "c"];
        }

        var arrayRecord = (JaggedArrayRecord<string>)SafePayloadReader.Read(Serialize(input));

        Assert.Equal((uint)input.Length, arrayRecord.Length);
        Assert.Equal(input, arrayRecord.ToArray());
        Assert.Equal(input, arrayRecord.ToArray(input.GetType()));
    }

    [Fact]
    public void CanReadJaggedArraysOfObjects()
    {
        object[][] input = new object[3][];
        for (int i = 0; i < input.Length; i++)
        {
            input[i] = ["a", 1, DateTime.MaxValue];
        }

        var arrayRecord = (JaggedArrayRecord<object>)SafePayloadReader.Read(Serialize(input));

        Assert.Equal((uint)input.Length, arrayRecord.Length);
        Assert.Equal(input, arrayRecord.ToArray());
        Assert.Equal(input, arrayRecord.ToArray(input.GetType()));
    }

    [Serializable]
    public class ComplexType
    {
        public int SomeField;
    }

    [Fact]
    public void CanReadJaggedArraysOfComplexTypes()
    {
        ComplexType[][] input = new ComplexType[3][];
        for (int i = 0; i < input.Length; i++)
        {
            input[i] = Enumerable.Range(0, i + 1).Select(j => new ComplexType { SomeField = j }).ToArray();
        }

        var arrayRecord = (JaggedArrayRecord<ClassRecord>)SafePayloadReader.Read(Serialize(input));

        Assert.Equal((uint)input.Length, arrayRecord.Length);
        var output = arrayRecord.ToArray();
        for (int i = 0; i < input.Length; i++)
        {
            for (int j = 0; j < input[i].Length; j++)
            {
                Assert.Equal(input[i][j].SomeField, output[i][j][nameof(ComplexType.SomeField)]);
            }
        }
    }
}
