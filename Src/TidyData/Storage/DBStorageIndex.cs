 #nullable disable
 using System.Collections.Immutable;

 namespace TidyData.Storage
{
    public class DBStorageIndex : IDBDocument
    {
        public DBStorageIndex(DBStorageIndex cloneFrom = null, Guid? id = null, DocumentVersion version = null,
            DocumentMetaData meta = null, ImmutableHashSet<string> dbSnapshotNameLog = null)
        {
            this.Id = id ?? cloneFrom?.Id ?? Guid.NewGuid();
            this.Version = version ?? cloneFrom?.Version ?? new DocumentVersion();
            this.Meta = meta ?? cloneFrom?.Meta ?? new DocumentMetaData();
            this.DBSnapshotNameLog = dbSnapshotNameLog ?? cloneFrom?.DBSnapshotNameLog ?? ImmutableHashSet<string>.Empty;
        }

        public Guid Id { get; }
        public DocumentVersion Version { get; init; }
        public DocumentMetaData Meta { get; init; }
        public ImmutableHashSet<string> DBSnapshotNameLog { get; }
    }

    public static class DBStorageIndexExtensions
    {
        public static string GetMostCurrentSnapshotLogEntry(this DBStorageIndex index)
        {
            string currentSnapshotLogName = index.GetSnapshotLogEntriesDateTimePriorityOrder()
                .FirstOrDefault();

            return currentSnapshotLogName;
        }

        public static IOrderedEnumerable<string> GetSnapshotLogEntriesDateTimePriorityOrder(this DBStorageIndex index)
        {
            return index.DBSnapshotNameLog
                .OrderByDescending(x => x, StringComparer.InvariantCulture);
        }
    }
}
