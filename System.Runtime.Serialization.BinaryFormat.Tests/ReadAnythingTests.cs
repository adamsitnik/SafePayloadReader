using System.Collections.Generic;
using System.IO;
using Xunit;
using System.Linq;

namespace System.Runtime.Serialization.BinaryFormat.Tests
{
    public class ReadAnythingTests : ReadTests
    {
        [Fact]
        public void UserCanReadAnyValidInputAndCheckTypesUsingStronglyTypedTypeInstances()
        {
            Dictionary<string, object> input = new()
            {
                { "exception", new Exception("test") },
                { "struct", new ValueTuple<bool, int>(true, 123) },
                { "generic", new List<int>(){ 1, 2, 3, 4 } }
            };

            using MemoryStream stream = Serialize(input);

            SerializationRecord topLevel = PayloadReader.Read(stream);

            Assert.IsAssignableFrom<ClassRecord>(topLevel);
            ClassRecord dictionaryRecord = (ClassRecord)topLevel;
            // this innocent line tests type forwards support ;)
            Assert.True(dictionaryRecord.IsTypeNameMatching(typeof(Dictionary<string, object>)));

            ClassRecord comparerRecord = dictionaryRecord.GetClassRecord(nameof(input.Comparer))!;
            Assert.True(comparerRecord.IsTypeNameMatching(input.Comparer.GetType()));

            ClassRecord[] keyValuePairs = dictionaryRecord.GetArrayOfClassRecords("KeyValuePairs")!;
            Assert.True(keyValuePairs[0].IsTypeNameMatching(typeof(KeyValuePair<string, object>)));

            ClassRecord exceptionPair = Find(keyValuePairs, "exception");
            ClassRecord exceptionValue = exceptionPair.GetClassRecord("value")!;
            Assert.True(exceptionValue.IsTypeNameMatching(typeof(Exception)));
            Assert.Equal("test", exceptionValue.GetString(nameof(Exception.Message)));

            ClassRecord structPair = Find(keyValuePairs, "struct");
            ClassRecord structValue = structPair.GetClassRecord("value")!;
            Assert.True(structValue.IsTypeNameMatching(typeof(ValueTuple<bool, int>)));
            Assert.True(structValue.GetBoolean("Item1"));
            Assert.Equal(123, structValue.GetInt32("Item2"));

            ClassRecord genericPair = Find(keyValuePairs, "generic");
            ClassRecord genericValue = genericPair.GetClassRecord("value")!;
            Assert.True(genericValue.IsTypeNameMatching(typeof(List<int>)));
            Assert.Equal(4, genericValue.GetInt32("_size"));
            Assert.Equal([1, 2, 3, 4], genericValue.GetArrayOfPrimitiveType<int>("_items"));

            static ClassRecord Find(ClassRecord[] keyValuePairs, string key)
                => keyValuePairs.Where(pair => pair.GetString("key") == key).Single();
        }
    }
}
