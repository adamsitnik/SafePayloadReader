using System;
using System.IO;
using System.Runtime.Serialization.BinaryFormat;
using System.Runtime.Serialization.Formatters.Binary;

namespace Playground
{
    [Serializable]
    public class ComplexType
    {
        public int Integer;
        public string? Text;
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            ComplexType input = new()
            {
                Integer = 123,
                Text = "Hello, World!"
            };

            using MemoryStream payload = Serialize(input);

            ClassRecord rootRecord = SafePayloadReader.ReadClassRecord<ComplexType>(payload);
            ComplexType output = new()
            {
                // using the indexer to read serialized primitive values
                Integer = rootRecord[nameof(ComplexType.Integer)] is int value ? value : default,
                Text = rootRecord[nameof(ComplexType.Text)] as string,
            };

            Console.WriteLine($"{output.Integer}, {output.Text}");
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
