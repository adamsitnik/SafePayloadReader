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

        ClassRecord classRecord = (ClassRecord)SafePayloadReader.Read(stream);

        // It's a surrogate, so there is no type match.
        Assert.False(classRecord.IsSerializedInstanceOf(typeof(Type)));
        Assert.Equal("System.UnitySerializationHolder", classRecord.TypeName);
        Assert.Equal("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", classRecord["AssemblyName"]);
    }
}
