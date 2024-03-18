using System;
using System.IO;
using System.Runtime.Serialization.BinaryFormat;
using System.Runtime.Serialization.Formatters.Binary;

namespace Playground
{
    internal class Program
    {
        static void Main()
        {
            ClassRecordDemo();

            WithArrayDemo();

            JaggedArrayDemo();
        }

        [Serializable]
        public class PrimitiveFields
        {
            public int Integer;
            public string? Text;
        }

        static void ClassRecordDemo()
        {
            PrimitiveFields input = new()
            {
                Integer = 123,
                Text = "Hello, World!"
            };

            using MemoryStream payload = Serialize(input);

            ClassRecord rootRecord = PayloadReader.ReadExactClassRecord<PrimitiveFields>(payload);
            PrimitiveFields output = new()
            {
                // using the indexer to read serialized primitive values
                Integer = rootRecord[nameof(PrimitiveFields.Integer)] is int value ? value : default,
                Text = rootRecord[nameof(PrimitiveFields.Text)] as string,
            };

            Console.WriteLine($"{output.Integer}, {output.Text}");
        }

        [Serializable]
        public class WithArray
        {
            public byte[]? ArrayOfBytes;
        }

        static void WithArrayDemo()
        {
            using MemoryStream payload = new ();
            WithArray input = new()
            {
                ArrayOfBytes = [0, 1, 2, 3]
            };

            new BinaryFormatter().Serialize(payload, input);
            payload.Position = 0;

            ClassRecord rootRecord = PayloadReader.ReadExactClassRecord<WithArray>(payload);
            WithArray output = new()
            {
                ArrayOfBytes = rootRecord[nameof(WithArray.ArrayOfBytes)] is ArrayRecord<byte> byteArray 
                    ? byteArray.ToArray() : default,
            };

            Console.WriteLine($"{string.Join(",", output.ArrayOfBytes!)}");
        }

        static void JaggedArrayDemo()
        {
            using MemoryStream payload = new();
            string[][]? input =
            [
                ["a", "b"],
                ["c", "d"]
            ];

            new BinaryFormatter().Serialize(payload, input);
            payload.Position = 0;

            ArrayRecord arrayRecord = PayloadReader.ReadAnyArrayRecord(payload);
            string[][] jaggedArray = (string[][])arrayRecord.ToArray(expectedArrayType: typeof(string[][]));
            foreach (string[] array in jaggedArray)
            {
                Console.WriteLine($"{string.Join(",", array)}");
            }
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
