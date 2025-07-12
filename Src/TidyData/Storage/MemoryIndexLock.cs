 #nullable disable
 using System.Collections.Concurrent;
 using NodaTime;
 using TidyUtility.Data.Json;

 namespace TidyData.Storage
{
    public class MemoryIndexLock : IIndexLock
    {
        private static readonly ConcurrentDictionary<string, string> NamedLockLookup =
            new ConcurrentDictionary<string, string>();

        private static readonly DBStorageIndex Empty = new DBStorageIndex(
            version: new DocumentVersion() { LastUpdatedUtc = Instant.MinValue });

        private readonly string _name;
        private readonly ISerializer _serializer;
        private readonly IClock _clock;
        private string _openDBStorageIndex;

        public MemoryIndexLock(string name, ISerializer serializer, IClock clock)
        {
            this._name = name;
            this._serializer = serializer;
            this._clock = clock;
        }

        // For Testing
        internal Task DeleteAsync()
        {
            string oldValue = NamedLockLookup.GetOrAdd(this._name, string.Empty);
            if (!NamedLockLookup.TryUpdate(this._name, string.Empty, oldValue))
                throw new StorageConcurrencyException();

            return Task.CompletedTask;
        }

        public Task<DBStorageIndex> ReadAndLockAsync()
        {
            this._openDBStorageIndex = NamedLockLookup.GetOrAdd(this._name, string.Empty);
            if (string.IsNullOrWhiteSpace(this._openDBStorageIndex))
                return Task.FromResult(Empty);
            return Task.FromResult(this._serializer.Deserialize<DBStorageIndex>(this._openDBStorageIndex));
        }

        public Task UpdateAndReleaseLockAsync(DBStorageIndex dbStorageIndex)
        {
            if (this._openDBStorageIndex == null)
                throw new InvalidOperationException($"Must call the {nameof(this.ReadAndLockAsync)}() method " +
                    $"before calling {nameof(this.UpdateAndReleaseLockAsync)}().");

            // Update version:
            dbStorageIndex = new DBStorageIndex(cloneFrom: dbStorageIndex,
                version: dbStorageIndex.Version.NewVersion(clock: this._clock));

            if (!NamedLockLookup.TryUpdate(this._name, this._serializer.Serialize(dbStorageIndex), this._openDBStorageIndex))
                throw new StorageConcurrencyException();

            this._openDBStorageIndex = null;

            return Task.CompletedTask;
        }

        public Task ReleaseLockAsync()
        {
            this._openDBStorageIndex = null;
            return Task.CompletedTask;
        }
    }
}