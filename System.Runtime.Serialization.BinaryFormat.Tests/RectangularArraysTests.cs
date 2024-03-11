﻿using System.IO;
using Xunit;

namespace System.Runtime.Serialization.BinaryFormat.Tests;

public class RectangularArraysTests : ReadTests
{
    [Fact]
    public void CanReadRectangularArraysOfPrimitiveTypes_2D()
    {
        int[,] array = new int[2,3];
        for (int i = 0; i < array.GetLength(0); i++)
        {
            for (int j = 0; j < array.GetLength(1); j++)
            {
                array[i, j] = i * j;
            }   
        }

        using MemoryStream stream = Serialize(array);

        SerializationRecord serializationRecord = SafePayloadReader.Read(stream);
    }

    [Fact]
    public void CanReadRectangularArraysOfStrings_2D()
    {
        string[,] array = new string[7, 4];
        for (int i = 0; i < array.GetLength(0); i++)
        {
            for (int j = 0; j < array.GetLength(1); j++)
            {
                array[i, j] = $"{i}, {j}";
            }
        }

        using MemoryStream stream = Serialize(array);

        SerializationRecord serializationRecord = SafePayloadReader.Read(stream);
    }

    [Fact]
    public void CanReadRectangularArraysOfObjects_2D()
    {
        object?[,] array = new object[6, 3];
        for (int i = 0; i < array.GetLength(0); i++)
        {
            array[i, 0] = i;
            array[i, 1] = $"{i}, 1";
            array[i, 2] = null;
        }

        using MemoryStream stream = Serialize(array);

        SerializationRecord serializationRecord = SafePayloadReader.Read(stream);
    }

    [Serializable]
    public class ComplexType2D
    {
        public int I, J;
    }

    [Fact]
    public void CanReadRectangularArraysOfComplexTypes_2D()
    {
        ComplexType2D[,] array = new ComplexType2D[3,7];
        for (int i = 0; i < array.GetLength(0); i++)
        {
            for (int j = 0; j < array.GetLength(1); j++)
            {
                array[i, j] = new() { I = i, J = j };
            }
        }

        using MemoryStream stream = Serialize(array);

        SerializationRecord serializationRecord = SafePayloadReader.Read(stream);
    }

    [Fact]
    public void CanReadRectangularArraysOfPrimitiveTypes_3D()
    {
        int[,,] array = new int[2, 3, 4];
        for (int i = 0; i < array.GetLength(0); i++)
        {
            for (int j = 0; j < array.GetLength(1); j++)
            {
                for (int k = 0; k < array.GetLength(2); k++)
                {
                    array[i, j, k] = i * j * k;
                }
            }
        }

        using MemoryStream stream = Serialize(array);

        SerializationRecord serializationRecord = SafePayloadReader.Read(stream);
    }

    [Fact]
    public void CanReadRectangularArraysOfStrings_3D()
    {
        string[,,] array = new string[9, 6, 3];
        for (int i = 0; i < array.GetLength(0); i++)
        {
            for (int j = 0; j < array.GetLength(1); j++)
            {
                for (int k = 0; k < array.GetLength(2); k++)
                {
                    array[i, j, k] = $"{i}, {j}, {k}";
                }
            }
        }

        using MemoryStream stream = Serialize(array);

        SerializationRecord serializationRecord = SafePayloadReader.Read(stream);
    }

    [Fact]
    public void CanReadRectangularArraysOfObjects_3D()
    {
        object?[,,] array = new object[6, 3, 1];
        for (int i = 0; i < array.GetLength(0); i++)
        {
            array[i, 0, 0] = i;
            array[i, 1, 0] = $"{i}, 1";
            array[i, 2, 0] = null;
        } 

        using MemoryStream stream = Serialize(array);

        SerializationRecord serializationRecord = SafePayloadReader.Read(stream);
    }

    [Serializable]
    public class ComplexType3D
    {
        public int I, J, K;
    }

    [Fact]
    public void CanReadRectangularArraysOfComplexTypes_3D()
    {
        ComplexType3D[,,] array = new ComplexType3D[3, 7, 11];
        for (int i = 0; i < array.GetLength(0); i++)
        {
            for (int j = 0; j < array.GetLength(1); j++)
            {
                for (int k = 0; k < array.GetLength(2); k++)
                {
                    array[i, j, k] = new() { I = i, J = j, K = k };
                }
            }
        }

        using MemoryStream stream = Serialize(array);

        SerializationRecord serializationRecord = SafePayloadReader.Read(stream);
    }
}
