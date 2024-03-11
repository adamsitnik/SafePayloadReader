using System.IO;
using Xunit;

namespace System.Runtime.Serialization.BinaryFormat.Tests;

public class CustomOffsetArrays : ReadTests
{
    [Fact]
    public void CanReadSingleDimensionalArrayOfIntegersWithCustomOffset()
    {
        const int lowerBound = 1;
        Array arrayOfIntegersIndexedFromOne = Array.CreateInstance(typeof(int),
            lengths: [3], lowerBounds: [lowerBound]);

        for (int i = lowerBound; i < lowerBound + arrayOfIntegersIndexedFromOne.Length; i++)
        {
            arrayOfIntegersIndexedFromOne.SetValue(value: i, index: i); 
        }

        using MemoryStream stream = Serialize(arrayOfIntegersIndexedFromOne);

        SerializationRecord serializationRecord = SafePayloadReader.Read(stream);
    }

    [Fact]
    public void CanReadRectangularArrayOfStringsWithCustomOffsets()
    {
        const int lowerBound = 10;
        Array jaggedArrayOfStringsIndexedFrom10 = Array.CreateInstance(typeof(string),
            lengths: [7, 5], lowerBounds: [lowerBound, lowerBound]);

        for (int i = lowerBound; i < lowerBound + jaggedArrayOfStringsIndexedFrom10.GetLength(0); i++)
        {
            for (int j = lowerBound; j < lowerBound + jaggedArrayOfStringsIndexedFrom10.GetLength(1); j++)
            {
                jaggedArrayOfStringsIndexedFrom10.SetValue(value: $"{i}. {j}", index1: i, index2: j);
            }
        }

        using MemoryStream stream = Serialize(jaggedArrayOfStringsIndexedFrom10);

        SerializationRecord serializationRecord = SafePayloadReader.Read(stream);
    }
}
