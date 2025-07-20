 #nullable disable
  namespace TidyData.Storage
{
    public interface IDBStorage<T>
        where T : class, new()
    {
        /// <summary>
        /// Read with no remaining lock held after call complete.
        /// </summary>
        /// <returns></returns>
        Task<T> ReadOnlyReadAsync();
        Task<T> ReadAndLockAsync();
        Task UpdateAndReleaseLockAsync(T dataModel);
        Task ReleaseLockAsync();
    }
}