 #nullable disable
 using System.Collections.Concurrent;
 using System.Collections.Immutable;
 using NodaTime;
 using TidyUtility.Data.Json;

 namespace TidyData.SnapshotLog
{
    /// <summary>
    /// An in memory snapshot set class. Primarily meant for unit testing.
    /// </summary>
    /// <typeparam name="T">The data type being managed.</typeparam>
    /// <seealso cref="AbstractSnapshotLog{T}" />
    public sealed class MemorySnapshotLog<T> : AbstractSnapshotLog<T>
        where T : new()
    {
        private static ConcurrentDictionary<string, ImmutableDictionary<string, string>> _savedSnapshotLookup =
            new ConcurrentDictionary<string, ImmutableDictionary<string, string>>();

        public MemorySnapshotLog(SnapshotLogSettings snapshotLogSettings, ISerializer serializer = null, IClock clock = null)
            : base(snapshotLogSettings, serializer, clock)
        {
        }

        protected override Task<string> SaveNewSnapshotAsync(T instanceToSave)
        {
            string snapshotName = this.BuildSnapshotName();
            string snapshotContents = this.Serializer.Serialize(instanceToSave);

            this.UpdateSnapshotLog(snapshotLogOrig => snapshotLogOrig.Add(snapshotName, snapshotContents));

            return Task.FromResult(snapshotName);
        }

        public override Task<T> LoadSnapshotAsync(string snapshotName)
        {
            if (!_savedSnapshotLookup.TryGetValue(this.SnapshotLogName, out ImmutableDictionary<string, string> snapshotLog))
                throw new SnapshotNotFoundException(snapshotName);

            if (!snapshotLog.TryGetValue(snapshotName, out string snapshotContents))
                throw new SnapshotNotFoundException(snapshotName);

            return Task.FromResult(this.Serializer.Deserialize<T>(snapshotContents));
        }

        public override Task DeleteAsync(string snapshotName)
        {
            this.UpdateSnapshotLog(snapshotLogOrig => snapshotLogOrig.Remove(snapshotName));

            return Task.CompletedTask;
        }

        public override Task DeleteAllAsync()
        {
            _savedSnapshotLookup.TryRemove(this.SnapshotLogName, out var ignored);
            return Task.CompletedTask;
        }

        public override async Task DeleteAllEligibleForAutoDeletionAsync()
        {
            List<string> snapshotNamesToDelete = 
                (await this.GetAllSnapshotNamesEligibleForAutoDeletionAsync())
                .ToList();

            this.UpdateSnapshotLog(snapshotLogOrig => snapshotLogOrig.RemoveRange(snapshotNamesToDelete));
        }

        public override Task<IEnumerable<string>> GetSavedSnapshotNamesAsync()
        {
            if (_savedSnapshotLookup.TryGetValue(this.SnapshotLogName, out ImmutableDictionary<string, string> snapshotLog))
                return Task.FromResult(snapshotLog.Keys);

            return Task.FromResult(Enumerable.Empty<string>());
        }

        private void UpdateSnapshotLog(Func<ImmutableDictionary<string, string>, ImmutableDictionary<string, string>> updateFunc)
        {
            bool success = false;
            int retriesRemaining = 10;
            while (!success && retriesRemaining-- > 0)
            {
                ImmutableDictionary<string, string> snapshotLogOrig = _savedSnapshotLookup
                    .GetOrAdd(this.SnapshotLogName, ImmutableDictionary<string, string>.Empty);

                ImmutableDictionary<string, string> snapshotLogUpdated = updateFunc(snapshotLogOrig);

                success = _savedSnapshotLookup.TryUpdate(this.SnapshotLogName, snapshotLogUpdated, snapshotLogOrig);
            }

            if (!success)
                throw new Exception($"Unable to save snapshot to {nameof(_savedSnapshotLookup)}.");
        }
    }
}