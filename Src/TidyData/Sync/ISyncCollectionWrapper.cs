 #nullable disable
 using NodaTime;

 namespace TidyData.Sync
{
    public interface ISyncCollectionWrapper
    {
        string CollectionName { get; }
        List<IDBDocument> GetUpdatedLocalRows(Instant? lastSync);
        void HandleLocalUpdateFromRemoteDocs(List<IDBDocument> rowsUpdatedOnRemote);
    }
}