 #nullable disable
 using NodaTime;
 using TidyData.SnapshotLog;

 namespace TidyData.Storage
{
    public abstract class DBStorageBase<T> : IDBStorage<T>
        where T : class, new()
    {
        protected abstract IClock Clock { get; }
        protected abstract ISnapshotLog<T> SnapshotLog { get; }
        protected abstract IIndexLock IndexLock { get; }
        private DBStorageIndex Index { get; set; }

        public async Task<T> ReadOnlyReadAsync()
        {
            T result = await this.ReadAndLockAsync();
            await this.ReleaseLockAsync();
            return result;
        }

        public async Task<T> ReadAndLockAsync()
        {
            this.Index = await this.IndexLock.ReadAndLockAsync();

            IOrderedEnumerable<string> indexEntries = this.Index.GetSnapshotLogEntriesDateTimePriorityOrder();
            foreach (string currentSnapshotLogEntry in indexEntries)
            {
                try
                {
                    return await this.SnapshotLog.LoadSnapshotAsync(currentSnapshotLogEntry);
                }
                catch (SnapshotNotFoundException)
                {
                    // Do nothing. Try the next one. If none in the index work,
                    // fallback to load the latest saved snapshot if it exists.
                }
            }
            
            return await this.SnapshotLog.LoadLastSavedSnapshotAsync();
        }

        public async Task UpdateAndReleaseLockAsync(T db)
        {
            if (this.Index == null)
                throw new InvalidOperationException($"Must call the {nameof(this.ReadAndLockAsync)}() method " +
                    $"before calling {nameof(this.UpdateAndReleaseLockAsync)}().");

            string newSnapshotName = await this.SnapshotLog.SaveSnapshotAsync(db);

            IEnumerable<string> snapshotsThatExist = await this.SnapshotLog.GetSavedSnapshotNamesAsync();
            var newLog = this.Index.DBSnapshotNameLog.Add(newSnapshotName).Intersect(snapshotsThatExist);
            this.Index = new DBStorageIndex(cloneFrom: this.Index, dbSnapshotNameLog: newLog);

            await this.IndexLock.UpdateAndReleaseLockAsync(this.Index);
            this.Index = null;
        }

        public async Task ReleaseLockAsync()
        {
            if (this.Index != null)
            {
                await this.IndexLock.ReleaseLockAsync();
                this.Index = null;
            }
        }

        /// <summary>
        /// Used for unit tests
        /// </summary>
        internal async Task DeleteDatabaseAsync()
        {
            await this.ReleaseLockAsync();

            DBStorageIndex index = await this.IndexLock.ReadAndLockAsync();
            await this.SnapshotLog.DeleteAllAsync();
            await this.IndexLock.UpdateAndReleaseLockAsync(new DBStorageIndex(cloneFrom: index,
                dbSnapshotNameLog: index.DBSnapshotNameLog.Clear()));
        }
    }
}