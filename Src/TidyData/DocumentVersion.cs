 #nullable disable
 using NodaTime;

 namespace TidyData
{
    public sealed record DocumentVersion : IComparable<DocumentVersion>
    {
        private Guid? _versionId;
        private Instant? _lastUpdatedUtc;

        // Override this record's copy constructor to insure any lazy initialization has completed before the copy
        private DocumentVersion(DocumentVersion original)
        {
            this.VersionId = original.VersionId;
            this.LastUpdatedUtc = original.LastUpdatedUtc;
        }

        public Guid VersionId
        {
            get => this._versionId ??= Guid.NewGuid();
            init => this._versionId = value;
        }

        public Instant LastUpdatedUtc
        {
            get => this._lastUpdatedUtc ??= SystemClock.Instance.GetCurrentInstant();
            init => this._lastUpdatedUtc = value;
        }

        #region IComparable<DocumentVersion> implementation and related operators

        public int CompareTo(DocumentVersion other)
        {
            if (other == null)
                return -1;

            int lastUpdatedCompare = this.LastUpdatedUtc.CompareTo(other.LastUpdatedUtc);
            if (lastUpdatedCompare != 0) 
                return lastUpdatedCompare;
            return this.VersionId.CompareTo(this.VersionId);
        }

        #endregion
    }

    public static class DocumentVersionExtensions
    {
        public static DocumentVersion NewVersion(this DocumentVersion cloneFrom, Guid? versionId = null, Instant? lastUpdatedUtc = null, IClock clock = null)
        {
            clock ??= SystemClock.Instance;

            if (cloneFrom == null)
                return new DocumentVersion() { VersionId = versionId ?? Guid.NewGuid(), LastUpdatedUtc = clock.GetCurrentInstant() };

            return cloneFrom with { VersionId = versionId ?? Guid.NewGuid(), LastUpdatedUtc = clock.GetCurrentInstant() };
        }
    }
}
