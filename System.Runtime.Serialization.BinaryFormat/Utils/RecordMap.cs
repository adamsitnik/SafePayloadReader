using System.Collections.Generic;
using System.Globalization;

namespace System.Runtime.Serialization.BinaryFormat;

internal sealed class RecordMap
{
    private readonly List<SerializationRecord> _records = new(); // TODO: verify whether we actually need that
#if NETCOREAPP
    private readonly Dictionary<int, SerializationRecord> _map = new(CollisionResistantInt32Comparer.Instance);
#else
    private readonly Dictionary<string, SerializationRecord> _map = new();
#endif

    internal void Add(SerializationRecord record)
    {
        _records.Add(record);

        // From https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-nrbf/0a192be0-58a1-41d0-8a54-9c91db0ab7bf:
        // "If the ObjectId is not referenced by any MemberReference in the serialization stream,
        // then the ObjectId SHOULD be positive, but MAY be negative."
        if (record.ObjectId != SerializationRecord.NoId)
        {
            // use Add on purpose, so in case of duplicate Ids we get an exception
#if NETCOREAPP
            _map.Add(record.ObjectId, record);
#else
            _map.Add(record.ObjectId.ToString(CultureInfo.InvariantCulture), record);
#endif
        }
    }

#if NETCOREAPP
    internal SerializationRecord this[int objectId] => _map[objectId];
#else
    internal SerializationRecord this[int objectId] => _map[objectId.ToString(CultureInfo.InvariantCulture)];
#endif

#if NETCOREAPP
    // keys (32-bit integer ids) are adversary-provided so we need a collision-resistant comparer
    private sealed class CollisionResistantInt32Comparer : IEqualityComparer<int>
    {
        internal static readonly CollisionResistantInt32Comparer Instance = new();

        private CollisionResistantInt32Comparer() { }

        public bool Equals(int x, int y) => x == y;

        public int GetHashCode(int obj) => HashCode.Combine(obj); // quick & dirty, but gets the job done
    }
#endif
}
