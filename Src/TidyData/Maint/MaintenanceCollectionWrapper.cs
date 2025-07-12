 #nullable disable
 using System.Collections.Immutable;
 using NodaTime;

 namespace TidyData.Maint
{
    public class MaintenanceCollectionWrapper<TDocument> : IMaintenanceCollectionWrapper
        where TDocument : IDBDocument
    {
        private readonly IDictionary<Guid, TDocument> _collection;
        private readonly Func<ImmutableHashSet<Guid>> _getReferencedAsForeignKeysFunc;

        public MaintenanceCollectionWrapper(IDictionary<Guid, TDocument> collection, Func<ImmutableHashSet<Guid>> getReferencedAsForeignKeysFunc)
        {
            this._collection = collection;
            this._getReferencedAsForeignKeysFunc = getReferencedAsForeignKeysFunc;
        }

        public void DeleteSoftDeletedDocsOlderThan(Instant instant)
        {
            ImmutableHashSet<Guid> idsReferencedAsForeignKeys = this._getReferencedAsForeignKeysFunc();

            List<TDocument> docsToDelete = this._collection.Values
                .Where(x => x.Meta.Deleted)
                .Where(x => x.Version.LastUpdatedUtc <= instant)
                .Where(x => !idsReferencedAsForeignKeys.Contains(x.Id))
                .ToList();

            foreach (TDocument doc in docsToDelete)
                this._collection.Remove(doc.Id);
        }
    }
}