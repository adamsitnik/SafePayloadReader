using System;
using System.Collections.Generic;
using System.Text;

namespace System.Runtime.Serialization.BinaryFormat;

/// <summary>
///  Record that marks the end of the binary format stream.
/// </summary>
public sealed class MessageEndRecord : SerializationRecord
{
    private static readonly MessageEndRecord _singleton = new();

    private MessageEndRecord()
    {
    }

    public override RecordType RecordType => RecordType.MessageEnd;

    internal static MessageEndRecord Parse()
    {
        // [MS-NRBF] 2.6.3
        // https://docs.microsoft.com/en-us/openspecs/windows_protocols/ms-nrbf/de6a574b-c596-4d83-9df7-63c0077acd32
        return _singleton;
    }
}
