using System;
using System.IO;
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

        static void Main()
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

            ClassRecord rootRecord = PayloadReader.ReadExactClassRecord<Sample>(payload);
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
