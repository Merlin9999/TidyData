 #nullable disable
using System;
using TidyData;

namespace TidySyncDB.UnitTests.TestModel
{
    public class TestDocumentWithForeignKey : IDBDocument
    {
        public TestDocumentWithForeignKey(TestDocumentWithForeignKey cloneFrom = null, Guid? id = null, DocumentVersion version = null, DocumentMetaData meta = null, Guid? testDocumentId = null)
        {
            this.Id = id ?? cloneFrom?.Id ?? Guid.NewGuid();
            this.Version = version ?? cloneFrom?.Version ?? DocumentVersionExtensions.NewVersion(null);
            this.Meta = meta ?? cloneFrom?.Meta ?? new DocumentMetaData();
            this.TestDocumentId = testDocumentId ?? cloneFrom?.TestDocumentId ?? Guid.Empty;
        }

        public Guid Id { get; }
        public DocumentVersion Version { get; init; }
        public DocumentMetaData Meta { get; init; }
        public Guid TestDocumentId { get; }
    }
}