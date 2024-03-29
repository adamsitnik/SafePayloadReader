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

        public static IEnumerable<object[]> GetAllInputTypes()
        {
            yield return new object[] { "string" };
            yield return new object[] { true };
            yield return new object[] { byte.MaxValue };
            yield return new object[] { sbyte.MaxValue };
            yield return new object[] { short.MaxValue };
            yield return new object[] { ushort.MaxValue };
            yield return new object[] { int.MaxValue };
            yield return new object[] { uint.MaxValue };
            yield return new object[] { long.MaxValue };
            yield return new object[] { ulong.MaxValue };
            yield return new object[] { float.MaxValue };
            yield return new object[] { double.MaxValue };
            yield return new object[] { decimal.MaxValue };
            yield return new object[] { TimeSpan.MaxValue };
            yield return new object[] { new DateTime(2000, 01, 01) };
            yield return new object[] { new Exception("SystemType") };
        }

        [Theory]
        [MemberData(nameof(GetAllInputTypes))]
        public void UserCanReadEveryPossibleSerializationRecord(object input)
        {
            SerializationRecord root = PayloadReader.Read(Serialize(input));

            switch(root)
            {
                case PrimitiveTypeRecord<string> stringRecord:
                    Assert.Equal(input, stringRecord.Value);
                    break;
                case PrimitiveTypeRecord<bool> record:
                    Assert.Equal(input, record.Value);
                    break;
                case PrimitiveTypeRecord<byte> record:
                    Assert.Equal(input, record.Value);
                    break;
                case PrimitiveTypeRecord<sbyte> record:
                    Assert.Equal(input, record.Value);
                    break;
                case PrimitiveTypeRecord<char> record:
                    Assert.Equal(input, record.Value);
                    break;
                case PrimitiveTypeRecord<short> record:
                    Assert.Equal(input, record.Value);
                    break;
                case PrimitiveTypeRecord<ushort> record:
                    Assert.Equal(input, record.Value);
                    break;
                case PrimitiveTypeRecord<int> record:
                    Assert.Equal(input, record.Value);
                    break;
                case PrimitiveTypeRecord<uint> record:
                    Assert.Equal(input, record.Value);
                    break;
                case PrimitiveTypeRecord<long> record:
                    Assert.Equal(input, record.Value);
                    break;
                case PrimitiveTypeRecord<ulong> record:
                    Assert.Equal(input, record.Value);
                    break;
                case PrimitiveTypeRecord<float> record:
                    Assert.Equal(input, record.Value);
                    break;
                case PrimitiveTypeRecord<double> record:
                    Assert.Equal(input, record.Value);
                    break;
                case PrimitiveTypeRecord<decimal> record:
                    Assert.Equal(input, record.Value);
                    break;
                case PrimitiveTypeRecord<DateTime> record:
                    Assert.Equal(input, record.Value);
                    break;
                case PrimitiveTypeRecord<TimeSpan> record:
                    Assert.Equal(input, record.Value);
                    break;
                case ClassRecord record when record.IsTypeNameMatching(typeof(Exception)):
                    Assert.Equal(((Exception)input).Message, record.GetString("Message"));
                    break;
                default:
                    Assert.Fail($"All cases should be handled! Record was {root.GetType()}, input was {input.GetType()}");
                    break;
            }
        }
    }
}
