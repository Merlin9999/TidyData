 #nullable disable
 using NodaTime;
 using TidyUtility.Core.Extensions;

 namespace TidyData.Sync
{
    public class SyncCollectionWrapper<TDocument> : ISyncCollectionWrapper
        where TDocument : IDBDocument
    {
        private readonly IDictionary<Guid, TDocument> _collection;

        public SyncCollectionWrapper(string collectionName, IDictionary<Guid, TDocument> collection)
        {
            this._collection = collection;
            this.CollectionName = collectionName;
        }

        public string CollectionName { get; }

        public List<IDBDocument> GetUpdatedLocalRows(Instant? lastSync)
        {
            return this._collection.Values
                .Where(x => lastSync == null || lastSync <= x.Version.LastUpdatedUtc)
                .Cast<IDBDocument>()
                .ToList();
        }

        public void HandleLocalUpdateFromRemoteDocs(List<IDBDocument> rowsUpdatedOnRemote)
        {
            foreach (IDBDocument dbDocument in rowsUpdatedOnRemote)
            {
                TDocument remoteVersionOfDoc = (TDocument)dbDocument;
                TDocument localVersionOfDoc = this._collection.TryGetValue(remoteVersionOfDoc.Id);

                if (localVersionOfDoc == null)
                    this.InsertLocalRow(remoteVersionOfDoc);
                else
                    this.UpdateLocalRow(localVersionOfDoc, remoteVersionOfDoc);
            }
        }

        private void InsertLocalRow(TDocument remoteVersionOfDoc)
        {
            this._collection.Add(remoteVersionOfDoc.Id, remoteVersionOfDoc);
        }

        private void UpdateLocalRow(TDocument localVersionOfDoc, TDocument remoteVersionOfDoc)
        {
            if (localVersionOfDoc.Version.LastUpdatedUtc > remoteVersionOfDoc.Version.LastUpdatedUtc)
            {
                this._collection[localVersionOfDoc.Id] = localVersionOfDoc;
            }
            if (localVersionOfDoc.Version.LastUpdatedUtc < remoteVersionOfDoc.Version.LastUpdatedUtc)
            {
                this._collection[remoteVersionOfDoc.Id] = remoteVersionOfDoc;
            }
        }
    }
}