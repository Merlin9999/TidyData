 #nullable disable
using System;
using Dynamitey;
using Newtonsoft.Json;

namespace TidySyncDB.UnitTests.TestModel
{
    //public class TestDocument : IDBDocument
    //{
    //    public TestDocument(TestDocument cloneFrom = null, Guid? id = null, DocumentVersion version = null, DocumentMetaData meta = null, string desc = null)
    //    {
    //        this.Id = id ?? cloneFrom?.Id ?? Guid.NewGuid();
    //        this.Version = version ?? cloneFrom?.Version ?? DocumentVersionExtensions.NewVersion(null);
    //        this.Meta = meta ?? cloneFrom?.Meta ?? new DocumentMetaData();
    //        this.Desc = desc ?? cloneFrom?.Desc ?? string.Empty;
    //    }

    //    public Guid Id { get; }
    //    public DocumentVersion Version { get; init; }
    //    public DocumentMetaData Meta { get; init; }
    //    public string Desc { get; }
    //}

    public record TestDocument : IDBDocument
    {
        public TestDocument(Guid? id = null, DocumentVersion version = null, DocumentMetaData meta = null, string desc = null)
        {
            this.Id = id ?? Guid.NewGuid();
            this.Version = version ?? DocumentVersionExtensions.NewVersion(null);
            this.Meta = meta ?? new DocumentMetaData();
            this.Desc = desc ?? string.Empty;
        }

        public Guid Id { get; init; } = Guid.NewGuid();
        public DocumentVersion Version { get; init; }
        public DocumentMetaData Meta { get; init; }
        public string Desc { get; init; }
    }
}