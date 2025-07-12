 #nullable disable
  namespace TidyData
{
    public abstract record AbstractDBDocument : IDBDocument
    {
        private Guid? _id;
        private DocumentVersion _version;
        private DocumentMetaData _meta;

        // Override this record's copy constructor to insure any lazy initialization has completed before the copy
        protected AbstractDBDocument(AbstractDBDocument original)
        {
            this.Id = original.Id;
            this.Version = original.Version;
            this.Meta = original.Meta;
        }

        public Guid Id
        {
            get => this._id ??= Guid.NewGuid();
            init => this._id = value;
        }

        public DocumentVersion Version
        {
            get => this._version ??= new DocumentVersion();
            init => this._version = value;
        }

        public DocumentMetaData Meta
        {
            get => this._meta ??= new DocumentMetaData();
            init => this._meta = value;
        }
    }
}
