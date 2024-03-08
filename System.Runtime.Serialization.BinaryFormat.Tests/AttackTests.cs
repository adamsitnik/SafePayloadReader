using System.IO;
using System.Text;
using System.Reflection;
using Xunit;
using System.Linq;
using System.Reflection.Emit;

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

    [Fact]
    public void UnboundedRecursion_NestedTypes_ActualBinaryFormatterInput()
    {
        Type[] ctorTypes = [typeof(string), typeof(Exception)];
        ConstructorInfo baseCtor = typeof(Exception).GetConstructor(ctorTypes)!;

        AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(new("Name"), AssemblyBuilderAccess.Run);
        ModuleBuilder module = assembly.DefineDynamicModule("PlentyOfExceptions");

        Exception previous = new("Some message");
        for (int i = 0; i <= 10_000; i++)
        {
            Exception nested = CreateNewExceptionTypeAndInstantiateIt(ctorTypes, baseCtor, module, previous, i);
            previous = nested;
        }

        using MemoryStream stream = Serialize(previous);

        SerializationRecord serializationRecord = SafePayloadReader.Read(stream);

        static Exception CreateNewExceptionTypeAndInstantiateIt(Type[] ctorTypes, ConstructorInfo baseCtor, 
            ModuleBuilder module, Exception previous, int i)
        {
#pragma warning disable SYSLIB0050 // Type or member is obsolete (I know!)
            const TypeAttributes publicSerializable = TypeAttributes.Public | TypeAttributes.Serializable;
#pragma warning restore SYSLIB0050

            var typeBuilder = module.DefineType($"Exception{i}", publicSerializable, typeof(Exception));
            var ctorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, ctorTypes);

            // generate a ctor that simply passes (string message, Exception innerException) to base type (Exception)
            ILGenerator ilGenerator = ctorBuilder.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ldarg_0); // push "this"
            ilGenerator.Emit(OpCodes.Ldarg_1); // push "message"
            ilGenerator.Emit(OpCodes.Ldarg_2); // push "innerException"
            ilGenerator.Emit(OpCodes.Call, baseCtor);
            ilGenerator.Emit(OpCodes.Ret);

            Type newExceptionType = typeBuilder.CreateType();

            ConstructorInfo constructorInfo = newExceptionType.GetConstructor(ctorTypes)!;

            return (Exception)constructorInfo.Invoke([i.ToString(), previous]);
        }
    }

    [Theory]
    [InlineData(RecordType.ClassWithMembers)]
    [InlineData(RecordType.ClassWithMembersAndTypes)]
    [InlineData(RecordType.SystemClassWithMembers)]
    [InlineData(RecordType.SystemClassWithMembersAndTypes)]
    public void UnboundedRecursion_NestedClasses_FakeButValidInput(RecordType recordType)
    {
        const int ClassesCount = 10_000;
        const int LibraryId = ClassesCount + 1;

        using MemoryStream stream = new();
        BinaryWriter writer = new(stream, Encoding.UTF8);

        WriteSerializedStreamHeader(writer);
        WriteBinaryLibrary(writer, LibraryId, "LibraryName");

        for (int i = 1; i <= ClassesCount; )
        {
            // ClassInfo (always present)
            writer.Write((byte)recordType);
            writer.Write(i); // object ID
            writer.Write($"Class{i}"); // type name
            bool isLast = i++ == ClassesCount;
            writer.Write(isLast ? 0 : 1); // member count (the last one has 0)

            if (!isLast)
            {
                writer.Write("memberName");
                // MemberTypeInfo (if needed)
                if (recordType is RecordType.ClassWithMembersAndTypes or RecordType.SystemClassWithMembersAndTypes)
                {
                    byte memberType = recordType is RecordType.SystemClassWithMembersAndTypes
                        ? (byte)3  // BinaryType.SystemClass
                        : (byte)4; // BinaryType.Class;

                    writer.Write(memberType);
                    writer.Write($"Class{i}"); // member type name

                    if (recordType is RecordType.ClassWithMembersAndTypes)
                    {
                        writer.Write(LibraryId);
                    }
                }
            }
            // LibraryId (if needed)
            if (recordType is RecordType.ClassWithMembers or RecordType.ClassWithMembersAndTypes)
            {
                writer.Write(LibraryId);
            }
        }

        writer.Write((byte)RecordType.MessageEnd);

        stream.Position = 0;

        SerializationRecord serializationRecord = SafePayloadReader.Read(stream);
    }
}
