 #nullable disable
 using NodaTime;
 using TidyData.SnapshotLog;
 using TidyUtility.Data.Json;

 namespace TidyData.Storage
{
    public sealed class MemoryDBStorage<T> : DBStorageBase<T>
        where T : class, new()
    {
        public MemoryDBStorage(SnapshotLogSettings settings, ISerializer serializer, IClock clock = null)
        {
            if (string.IsNullOrWhiteSpace(settings.SnapshotLogName))
                throw new ArgumentException($"{nameof(SnapshotLogSettings)}.{nameof(settings.SnapshotLogName)} must not be null or empty!", nameof(settings.SnapshotLogName));

            this.Clock = clock ?? SystemClock.Instance;
            this.SnapshotLog = new MemorySnapshotLog<T>(settings, serializer, this.Clock);
            this.IndexLock = new MemoryIndexLock(settings.SnapshotLogName, serializer, clock);
        }

        protected override IClock Clock { get; }
        protected override ISnapshotLog<T> SnapshotLog { get; }
        protected override IIndexLock IndexLock { get; }
    }
}