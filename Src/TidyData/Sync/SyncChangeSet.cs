 #nullable disable
 using NodaTime;
 using TidyUtility.Data.Json;

 namespace TidyData.Sync
{
    [SafeToSerialize(IncludeNestedDerived = true)]
    public class SyncChangeSet
    {
        public Instant SyncStart { get; set; }
        public Instant? LastSync { get; set; }
        public Guid ClientDeviceId { get; set; }
        public string ClientDeviceName { get; set; }
        public Guid AccountId { get; set; }
        public List<CollectionChangeSet> ChangedCollections { get; set; }
    }

    public class CollectionChangeSet
    {
        public string CollectionName { get; set; }
        public List<IDBDocument> ChangedDocuments { get; set; }
    }

    public static class SyncChangeSetExtensions
    {
        public static HashSet<DocumentVersion> GetDocumentVersions(this SyncChangeSet changeSet)
        {
            return changeSet.ChangedCollections
                .SelectMany(x => x.ChangedDocuments)
                .Select(x => x.Version)
                .ToHashSet();
        }
    }
}