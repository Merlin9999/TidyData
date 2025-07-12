 #nullable disable
  namespace TidyData.Storage
{
    public class CacheDBStorageAdapter<T> : IDBStorage<T>
        where T : class, new()
    {
        internal readonly IDBStorage<T> _storageImpl;
        private volatile T _cache = null;

        public CacheDBStorageAdapter(IDBStorage<T> storageImpl)
        {
            this._storageImpl = storageImpl;
        }

        public async Task<T> ReadOnlyReadAsync()
        {
            T dataModel = this._cache;

            if (dataModel != null)
                return dataModel;

            dataModel = await this._storageImpl.ReadOnlyReadAsync();
            this._cache = dataModel;
            return dataModel;
        }

        public async Task<T> ReadAndLockAsync()
        {
            T dataModel = await this._storageImpl.ReadAndLockAsync();
            this._cache = dataModel;
            return dataModel;
        }

        public async Task UpdateAndReleaseLockAsync(T db)
        {
            await this._storageImpl.UpdateAndReleaseLockAsync(db);
            this._cache = db;
        }

        public async Task ReleaseLockAsync()
        {
            await this._storageImpl.ReleaseLockAsync();
        }
    }
}
