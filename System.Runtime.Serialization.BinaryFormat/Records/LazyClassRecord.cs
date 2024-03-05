using System.Collections.Generic;

namespace System.Runtime.Serialization.BinaryFormat;

/// <summary>
/// In certain scenarios we know that given <seealso cref="MemberReferenceRecord"/> references a Class Record
/// (example: <seealso cref="BinaryArrayRecord"/> which is basically an array of T, so we know all elements are T),
/// but the referenced record has not been parsed yet.
/// This type is a lazy <seealso cref="ClassRecord"/> wrapper for <seealso cref="MemberReferenceRecord"/>.
/// It de-references the actual <seealso cref="ClassRecord"/> when needed for the first time.
/// </summary>
internal sealed class LazyClassRecord : ClassRecord
{
    internal LazyClassRecord(MemberReferenceRecord referenceRecord) : base(null!, null!) => ReferenceRecord = referenceRecord;

    public override RecordType RecordType => GetClassRecord().RecordType;

    public override IReadOnlyList<string> MemberNames => GetClassRecord().MemberNames;

    internal override int ObjectId => GetClassRecord().ObjectId;

    internal override IReadOnlyList<object> MemberValues => GetClassRecord().MemberValues;

    private MemberReferenceRecord ReferenceRecord { get; }

    private ClassRecord? LazyLoaded { get; set; }

    public override object? this[string memberName] => GetClassRecord()[memberName];

    private ClassRecord GetClassRecord()
        => LazyLoaded ??= (ClassRecord)ReferenceRecord.GetReferencedRecord();
}
