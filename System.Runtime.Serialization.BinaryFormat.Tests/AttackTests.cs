using System.IO;
using System.Text;
using System.Reflection;
using Xunit;
using System.Linq;

namespace System.Runtime.Serialization.BinaryFormat.Tests;

public class AttackTests : ReadTests
{
    [Serializable]
    public class WithCyclicReference
    {
        public string? Name;
        public WithCyclicReference? ReferenceToSelf;
    }

    [Fact]
    public void CyclicReferencesInClassesDoNotCauseStackOverflow()
    {
        WithCyclicReference input = new();
        input.Name = "hello";
        input.ReferenceToSelf = input;

        using MemoryStream stream = Serialize(input);

        ClassRecord classRecord = SafePayloadReader.ReadClassRecord<WithCyclicReference>(stream);

        Assert.Same(classRecord, classRecord[nameof(WithCyclicReference.ReferenceToSelf)]);
        Assert.Equal(input.Name, classRecord[nameof(WithCyclicReference.Name)]);
    }

    [Fact]
    public void CyclicReferencesInSystemClassesDoNotCauseStackOverflow()
    {
        // CoreLib types are represented using a different record, that is why we need a dedicated test
        Exception input = new("hello");

        // set a reference to self by using private field
        typeof(Exception).GetField("_innerException", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(input, input);

        using MemoryStream stream = Serialize(input);

        ClassRecord classRecord = SafePayloadReader.ReadClassRecord<Exception>(stream);

        Assert.Same(classRecord, classRecord[nameof(Exception.InnerException)]);
        Assert.Equal(input.Message, classRecord[nameof(Exception.Message)]);
    }

    [Fact]
    public void CyclicReferencesInArraysOfObjectsDoNotCauseStackOverflow()
    {
        object[] input = new object[2];
        input[0] = "not an array";
        input[1] = input;

        using MemoryStream stream = Serialize(input);

        object?[] output = SafePayloadReader.ReadArrayOfObjects(stream);

        Assert.Equal(input[0], output[0]);
        Assert.Same(input, input[1]);
        Assert.Same(output, output[1]);
    }

    [Serializable]
    public class WithCyclicReferenceInArrayOfObjects
    {
        public string? Name;
        public object?[]? ArrayWithReferenceToSelf;
    }

    [Fact]
    public void CyclicClassReferencesInArraysOfObjectsDoNotCauseStackOverflow()
    {
        WithCyclicReferenceInArrayOfObjects input = new();
        input.Name = "hello";
        input.ArrayWithReferenceToSelf = [input];

        using MemoryStream stream = Serialize(input);

        ClassRecord classRecord = SafePayloadReader.ReadClassRecord<WithCyclicReferenceInArrayOfObjects>(stream);

        Assert.Equal(input.Name, classRecord[nameof(WithCyclicReferenceInArrayOfObjects.Name)]);
        ArrayRecord<object?> array = (ArrayRecord<object?>)classRecord[nameof(WithCyclicReferenceInArrayOfObjects.ArrayWithReferenceToSelf)]!;
        Assert.Same(classRecord, array.Deserialize().Single());
    }

    [Serializable]
    public class WithCyclicReferenceInArrayOfT
    {
        public string? Name;
        public WithCyclicReferenceInArrayOfT?[]? ArrayWithReferenceToSelf;
    }

    [Fact]
    public void CyclicClassReferencesInArraysOfTDoNotCauseStackOverflow()
    {
        WithCyclicReferenceInArrayOfT input = new();
        input.Name = "hello";
        input.ArrayWithReferenceToSelf = [input];

        using MemoryStream stream = Serialize(input);

        ClassRecord classRecord = SafePayloadReader.ReadClassRecord<WithCyclicReferenceInArrayOfT>(stream);

        Assert.Equal(input.Name, classRecord[nameof(WithCyclicReferenceInArrayOfT.Name)]);
        object?[] array = (object?[])classRecord[nameof(WithCyclicReferenceInArrayOfT.ArrayWithReferenceToSelf)]!;
        Assert.Same(classRecord, array.Single());
    }

    [Fact]
    public void ArraysOfStringsAreNotBeingPreAllocated()
    {
        using MemoryStream stream = new();
        BinaryWriter writer = new(stream, Encoding.UTF8);

        WriteSerializedStreamHeader(writer);

        writer.Write((byte)RecordType.ArraySingleString);
        writer.Write(1); // object ID
        writer.Write(Array.MaxLength); // length
        writer.Write((byte)RecordType.ObjectNullMultiple);
        writer.Write(Array.MaxLength); // null count
        writer.Write((byte)RecordType.MessageEnd);

        stream.Position = 0;

        long before = GetAllocatedByteCount();

        SerializationRecord serializationRecord = SafePayloadReader.Read(stream);

        long after = GetAllocatedByteCount();

        Assert.InRange(after, before, before + 1024);
        Assert.Equal(RecordType.ArraySingleString, serializationRecord.RecordType);

        static long GetAllocatedByteCount()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            return GC.GetAllocatedBytesForCurrentThread();
        }
    }
}
