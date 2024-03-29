using System.IO;
using Xunit;

namespace System.Runtime.Serialization.BinaryFormat.Tests;

public class EdgeCaseTests : ReadTests
{
    [Fact]
    public void SurrogatesGetNoSpecialHandling()
    {
#if NETCOREAPP
        // Type is [Serializable] only on Full .NET Framework.
        // So here we use a Base64 representation of serialized typeof(object)
        const string serializedWithFullFramework = "AAEAAAD/////AQAAAAAAAAAEAQAAAB9TeXN0ZW0uVW5pdHlTZXJpYWxpemF0aW9uSG9sZGVyAwAAAAREYXRhCVVuaXR5VHlwZQxBc3NlbWJseU5hbWUBAAEIBgIAAAANU3lzdGVtLk9iamVjdAQAAAAGAwAAAEttc2NvcmxpYiwgVmVyc2lvbj00LjAuMC4wLCBDdWx0dXJlPW5ldXRyYWwsIFB1YmxpY0tleVRva2VuPWI3N2E1YzU2MTkzNGUwODkL";

        using MemoryStream stream = new(Convert.FromBase64String(serializedWithFullFramework));
#else
        using MemoryStream stream = Serialize(typeof(object));
#endif

        ClassRecord classRecord = (ClassRecord)PayloadReader.Read(stream);

        // It's a surrogate, so there is no type match.
        Assert.False(classRecord.IsTypeNameMatching(typeof(Type)));
        Assert.Equal("System.UnitySerializationHolder", classRecord.TypeName);
        Assert.Equal("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", classRecord.GetString("AssemblyName"));
    }

    [Fact]
    public void ArraysOfStringsCanContainMemberReferences()
    {
        // it has to be the same object, not just the same value
        const string same = "same";
        string[] input = { same, same };

        using MemoryStream stream = Serialize(input);

        string?[] ouput = ((ArrayRecord<string>)PayloadReader.Read(stream)).ToArray();

        Assert.Equal(input, ouput);
        Assert.Same(ouput[0], ouput[1]);
    }

    [Theory]
    [InlineData(100)]
    [InlineData(64_001)]
    [InlineData(127_000)]
#if RELEASE // it takes a lot of time to execute (+- 4 minutes)
    [InlineData(2147483591)] // Array.MaxLength
#endif
    public void CanReadArrayOfAnySize(int length)
    {
        byte[] input = new byte[length]; 
        new Random().NextBytes(input);

        // MemoryStream can not handle large array payloads as it's backed by an array.
        using FileStream stream = SerializeToFile(input);

        byte[] output = ((ArrayRecord<byte>)PayloadReader.Read(stream)).ToArray(maxLength: length);
        Assert.Equal(input, output);
    }
}
