 #nullable disable
 using NodaTime;
 using TidyData.SnapshotLog;
 using TidyUtility.Data.Json;

 namespace TidyData.Storage
{
    public sealed class FileDBStorage<T> : DBStorageBase<T>
        where T : class, new()
    {
        public FileDBStorage(SnapshotLogSettings settings, string pathToSnapshotLogFolder, ISerializer serializer, IClock clock = null)
        {
            this.Clock = clock ?? SystemClock.Instance;
            this.SnapshotLog = new FileSnapshotLog<T>(settings, pathToSnapshotLogFolder, serializer, clock);
            string indexLockFileName = Path.Combine(pathToSnapshotLogFolder, this.BuildIndexLogFileName());
            this.IndexLock = new FileIndexLock(indexLockFileName, serializer, clock);
        }
        
        protected override IClock Clock { get; }
        protected override ISnapshotLog<T> SnapshotLog { get; }
        protected override IIndexLock IndexLock { get; }

        private string BuildIndexLogFileName()
        {
            return $"{this.SnapshotLog.Settings.SnapshotLogName}_Index{this.SnapshotLog.Settings.FileExtension}";
        }
    }
}