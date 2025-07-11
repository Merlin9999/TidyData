 #nullable disable
  namespace TidyData.SnapshotLog
{

    public interface ISnapshotLog<T>
        where T : new()
    {
        SnapshotLogSettings Settings { get; }
        Task<IEnumerable<string>> GetSavedSnapshotNamesAsync();
        Task<string> GetMostCurrentSnapshotAsync();
        Task<string> SaveSnapshotAsync(T instanceToSave);
        Task<T> LoadLastSavedSnapshotAsync();
        Task<T> LoadSnapshotAsync(string snapshotName);
        Task DeleteAsync(string snapshotName);
        Task DeleteAllAsync();
        Task DeleteAllEligibleForAutoDeletionAsync();
    }
}