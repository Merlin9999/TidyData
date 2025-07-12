 #nullable disable
 using NodaTime;
 using TidyUtility.Data.Json;

 namespace TidyData.Storage
{
    public class FileIndexLock : IIndexLock
    {
        private readonly string _fileName;
        private readonly ISerializer _serializer;
        private readonly IClock _clock;
        private bool _fileIsOpen;
        private FileStream _fileStream;

        public  FileIndexLock(string fileName, ISerializer serializer, IClock clock)
        {
            this._fileName = fileName;
            this._serializer = serializer;
            this._clock = clock;
        }

        // For Testing ONLY
        internal async Task DeleteAsync()
        {
            if (this._fileIsOpen)
                await this.ReleaseLockAsync();

            if (File.Exists(this._fileName))
                File.Delete(this._fileName);
        }

        public async Task<DBStorageIndex> ReadAndLockAsync()
        {
            bool fileExisted = File.Exists(this._fileName);

            if (!this._fileIsOpen)
            {
                try
                {
                    this._fileStream = new FileStream(this._fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                }
                catch (IOException exc) when (exc.HResult == -2147024864) // Sharing Violation
                {
                    throw new StorageConcurrencyException(exc);
                }

                this._fileIsOpen = true;
            }

            if (!fileExisted)
                return new DBStorageIndex(version: new DocumentVersion() { LastUpdatedUtc = Instant.MinValue });

            this._fileStream.Seek(0, SeekOrigin.Begin);

            var sr = new StreamReader(this._fileStream);
            string indexString = await sr.ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(indexString))
                return new DBStorageIndex(version: new DocumentVersion() { LastUpdatedUtc = Instant.MinValue });

            return this._serializer.Deserialize<DBStorageIndex>(indexString);
        }

        public async Task UpdateAndReleaseLockAsync(DBStorageIndex dbStorageIndex)
        {
            if (!this._fileIsOpen)
                throw new InvalidOperationException($"Must call the {nameof(this.ReadAndLockAsync)}() method " +
                    $"before calling {nameof(this.UpdateAndReleaseLockAsync)}().");

            using (this._fileStream)
            {
                this._fileStream.Seek(0, SeekOrigin.Begin);
                this._fileStream.SetLength(0);

                // Update version:
                dbStorageIndex = new DBStorageIndex(cloneFrom: dbStorageIndex,
                    version: dbStorageIndex.Version.NewVersion(clock: this._clock));

                var sw = new StreamWriter(this._fileStream);
                await sw.WriteAsync(this._serializer.Serialize(dbStorageIndex));
                await sw.FlushAsync();
                await this.ReleaseLockAsync();
            }

        }

        public Task ReleaseLockAsync()
        {
            if (this._fileIsOpen)
            {
                this._fileIsOpen = false;
                this._fileStream.Close();
            }

            return Task.CompletedTask;
        }
    }
}