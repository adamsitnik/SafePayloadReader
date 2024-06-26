﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.Serialization.BinaryFormat;

/// <summary>
///  Record that marks the end of the binary format stream.
/// </summary>
/// <remarks>
///  <para>
///   <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/ms-nrbf/de6a574b-c596-4d83-9df7-63c0077acd32">
///    [MS-NRBF] 2.6.3
///   </see>
///  </para>
/// </remarks>
internal sealed class MessageEndRecord : SerializationRecord
{
    internal static MessageEndRecord Singleton { get; } = new();

    private MessageEndRecord()
    {
    }

    public override RecordType RecordType => RecordType.MessageEnd;
}
