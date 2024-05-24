using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.BinaryFormat;
using System.Runtime.Serialization.Formatters.Binary;

namespace Playground
{
    internal class Program
    {
        [Serializable]
        public class Sample
        {
            public int Integer;
            public string? Text;
            public byte[]? ArrayOfBytes;
            public Sample? ClassInstance;
        }

        [Serializable]
        public class MoreComplex
        {
            public Guid Id { get; set; }
            public FileAccess Enumeration { get; set; }
            public Point Astruct { get; set; }
        }

        static void Main()
        {
            ReadClass();
            ReadMoreComplexType();
            SwitchLike();
            ReadArrayOfClasses();
        }

        static void ReadClass()
        {
            Sample input = new()
            {
                Integer = 123,
                Text = "Hello, World!",
                ArrayOfBytes = [0, 1, 2, 3],
                ClassInstance = new()
                {
                    Text = "ClassRecord"
                }
            };

            using MemoryStream payload = Serialize(input);

            ClassRecord rootRecord = PayloadReader.ReadClassRecord(payload);
            Sample output = new()
            {
                // using the dedicated methods to read primitive values
                Integer = rootRecord.GetInt32(nameof(Sample.Integer)),
                Text = rootRecord.GetString(nameof(Sample.Text)),
                // using dedicated method to read an array of bytes
                ArrayOfBytes = rootRecord.GetArrayOfPrimitiveType<byte>(nameof(Sample.ArrayOfBytes)),
                // using GetClassRecord to read a class record
                ClassInstance = new()
                {
                    Text = rootRecord
                        .GetClassRecord(nameof(Sample.ClassInstance))!
                        .GetString(nameof(Sample.Text))
                }
            };

            Console.WriteLine($"{output.Integer}, {output.Text}");
            Console.WriteLine($"{string.Join(",", output.ArrayOfBytes!)}");
            Console.WriteLine($"{output.ClassInstance.Text}");
        }

        static void ReadMoreComplexType()
        {
            MoreComplex input = new()
            {
                Id = Guid.NewGuid(),
                Enumeration = FileAccess.ReadWrite,
                Astruct = new Point(x: 1, y: 2)
            };

            using MemoryStream payload = Serialize(input);

            ClassRecord rootRecord = PayloadReader.ReadClassRecord(payload);

            // We need to use the field, not property name to get the value: https://review.learn.microsoft.com/en-us/dotnet/standard/serialization/binaryformatter-migration-guide/functionality-reference?branch=binaryformatter-migration-guide#member-names
            ClassRecord guidRecord = rootRecord.GetClassRecord("<Id>k__BackingField")!;
            // Guid is a very specific example where reading something simple takes a lot of work
            // like checking the member types in https://github.com/dotnet/runtime/blob/f4c9264fe8448fdf1f66eda04a582cbade40cd39/src/libraries/System.Private.CoreLib/src/System/Guid.cs#L33-L43
            Guid guid = new Guid
            (
                a: guidRecord.GetInt32("_a"),
                b: guidRecord.GetInt16("_b"),
                c: guidRecord.GetInt16("_c"),
                d: guidRecord.GetByte("_d"),
                e: guidRecord.GetByte("_e"),
                f: guidRecord.GetByte("_f"),
                g: guidRecord.GetByte("_g"),
                h: guidRecord.GetByte("_h"),
                i: guidRecord.GetByte("_i"),
                j: guidRecord.GetByte("_j"),
                k: guidRecord.GetByte("_k")
            );
            ClassRecord structRecord = rootRecord.GetClassRecord("<Astruct>k__BackingField")!;
            Point point = new Point
            (
                x: structRecord.GetInt32("x"),
                y: structRecord.GetInt32("y")
            );

            MoreComplex output = new()
            {
                Id = guid,
                // enums are represented as ClassRecords with a single field "value__"
                Enumeration = (FileAccess)rootRecord.GetClassRecord("<Enumeration>k__BackingField")!.GetInt32("value__"),
                Astruct = point
            };

            Console.WriteLine(output.Id);
            Console.WriteLine(output.Enumeration);
            Console.WriteLine(output.Astruct);
        }

        static void SwitchLike()
        {
            object input = Random.Shared.Next(0, 3) switch
            {
                0 => "text",
                1 => new byte[] { 0, 1, 2, 3 },
                _ => new KeyValuePair<string, int>("demo", 5)
            };

            using MemoryStream payload = Serialize(input);
            SerializationRecord rootObject = PayloadReader.Read(payload);

            if (rootObject is PrimitiveTypeRecord<string> stringRecord)
            {
                Console.WriteLine($"It was a string: '{stringRecord.Value}'");
            }
            else if (rootObject is ArrayRecord<byte> arrayOfBytes)
            {
                Console.WriteLine($"It was an array of bytes: '{string.Join(",", arrayOfBytes.ToArray())}'");
            }
            else if (rootObject is ClassRecord classRecord)
            {
                Console.WriteLine($"It was a class record of '{classRecord.TypeName}' type.");
            }
        }

        static void ReadArrayOfClasses()
        {
            Sample[] input = new[]
            {
                new Sample()
                {
                    Integer = 1,
                    Text = "First"
                },
                new Sample()
                {
                    Integer = 2,
                    Text = "Second"
                },
            };

            using MemoryStream payload = Serialize(input);

            ArrayRecord<ClassRecord> rootRecord = (ArrayRecord<ClassRecord>)PayloadReader.Read(payload);
            ClassRecord[] classRecords = rootRecord.ToArray(allowNulls: false)!;
            Sample[] output = classRecords
                .Select(classRecord => new Sample()
                {
                    Integer = classRecord.GetInt32(nameof(Sample.Integer)),
                    Text = classRecord.GetString(nameof(Sample.Text))
                })
                .ToArray();
        }

        static MemoryStream Serialize<T>(T instance) where T : notnull
        {
            MemoryStream ms = new();
            BinaryFormatter binaryFormatter = new();
            binaryFormatter.Serialize(ms, instance);

            ms.Position = 0;
            return ms;
        }
    }
}
