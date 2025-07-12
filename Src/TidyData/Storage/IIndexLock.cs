 #nullable disable
  namespace TidyData.Storage
{
    public interface IIndexLock
    {
        Task<DBStorageIndex> ReadAndLockAsync();
        Task UpdateAndReleaseLockAsync(DBStorageIndex dbStorageIndex);
        Task ReleaseLockAsync();
    }
}