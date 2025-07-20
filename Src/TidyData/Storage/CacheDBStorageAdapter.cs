 #nullable disable
  namespace TidyData.Storage
{
    public class CacheDBStorageAdapter<T> : IDBStorage<T>
        where T : class, new()
    {
        internal readonly IDBStorage<T> StorageImpl;
        private volatile T _dataModelCache = null;

        public CacheDBStorageAdapter(IDBStorage<T> storageImpl)
        {
            this.StorageImpl = storageImpl;
        }

        public async Task<T> ReadOnlyReadAsync()
        {
            T dataModel = this.DataModelCache;

            if (dataModel != null)
                return dataModel;

            dataModel = await this.StorageImpl.ReadOnlyReadAsync();
            this.DataModelCache = dataModel;
            return dataModel;
        }

        public async Task<T> ReadAndLockAsync()
        {
            T dataModel = await this.StorageImpl.ReadAndLockAsync();
            this.DataModelCache = dataModel;
            return dataModel;
        }

        public async Task UpdateAndReleaseLockAsync(T dataModel)
        {
            await this.StorageImpl.UpdateAndReleaseLockAsync(dataModel);
            this.DataModelCache = dataModel;
        }

        public async Task ReleaseLockAsync()
        {
            await this.StorageImpl.ReleaseLockAsync();
        }

        private T DataModelCache
        {
            get => Interlocked.CompareExchange(ref _dataModelCache, null, null);
            set => Interlocked.Exchange(ref _dataModelCache, value);
        }

    }
}
