using System.IO;
using System.Text;
using Xunit;

namespace System.Runtime.Serialization.BinaryFormat.Tests;

public class EdgeCaseTests : ReadTests
{
    [Fact]
    public void ThrowsOnInvalidUtf8Input()
    {
        using MemoryStream stream = new();
        BinaryWriter writer = new(stream, Encoding.UTF8);

        WriteSerializedStreamHeader(writer, major: 1, minor: 0);

        byte[] invalidUtf8 = [(byte)'a', (byte)'b', 0xC0, (byte)'x', (byte)'y'];

        writer.Write((byte)RecordType.BinaryObjectString);
        writer.Write((int)1); // object ID
        writer.Write7BitEncodedInt(invalidUtf8.Length);
        writer.Write(invalidUtf8);
        writer.Write((byte)RecordType.MessageEnd);

        stream.Position = 0;
        Assert.Throws<DecoderFallbackException>(() => SafePayloadReader.Read(stream));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    public void ThrowsOnInvalidHeaderVersion(int major, int minor)
    {
        using MemoryStream stream = new();
        BinaryWriter writer = new(stream, Encoding.UTF8);

        WriteSerializedStreamHeader(writer, major, minor);

        stream.Position = 0;
        Assert.Throws<SerializationException>(() => SafePayloadReader.Read(stream));
    }

    [Serializable]
    public class WithCyclicReference
    {
        public string? Name;
        public WithCyclicReference? ReferenceToSelf;
    }

    [Fact]
    public void CyclicReferencesAreSupported()
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

        ClassRecord classRecord = (ClassRecord)SafePayloadReader.Read(stream);

        // It's a surrogate, so there is no type match.
        Assert.False(classRecord.IsSerializedInstanceOf(typeof(Type)));
        Assert.Equal("System.UnitySerializationHolder", classRecord.TypeName);
        Assert.Equal("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", classRecord["AssemblyName"]);
    }

    private static void WriteSerializedStreamHeader(BinaryWriter writer, int major, int minor)
    {
        writer.Write((byte)RecordType.SerializedStreamHeader);
        writer.Write(1); // root ID
        writer.Write(1); // header ID
        writer.Write(major); // major version
        writer.Write(minor); // minor version
    }
}
