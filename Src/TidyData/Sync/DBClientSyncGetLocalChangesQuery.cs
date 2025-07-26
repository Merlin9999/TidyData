 #nullable disable
 using NodaTime;

 namespace TidyData.Sync
{
    public class DBClientSyncGetLocalChangesQuery<TClientDataModel> : IQuery<TClientDataModel, SyncChangeSet>
        where TClientDataModel : ISyncClientDataModel, new()
    {
        private readonly Instant _lastDeviceSyncTimeFromServer;
        private readonly Guid _accountId;
        private readonly Guid _deviceId;
        private readonly string _deviceName;

        public DBClientSyncGetLocalChangesQuery(Instant lastDeviceSyncTimeFromServer, Guid accountId, Guid deviceId, string deviceName)
        {
            this._lastDeviceSyncTimeFromServer = lastDeviceSyncTimeFromServer;
            this._accountId = accountId;
            this._deviceId = deviceId;
            this._deviceName = deviceName;
        }

        public SyncChangeSet Execute(TClientDataModel model, CollectionWrapperFactory factory)
        {
            SyncChangeSet clientChangeSet = this.BuildEmptySyncChangeSet(model, factory.Clock);
            List<ICollectionWrapper> localClientCollections = factory.GetCollections(model).ToList();
            this.LoadCollectionChangeSets(clientChangeSet, localClientCollections);

            return clientChangeSet;
        }

        private SyncChangeSet BuildEmptySyncChangeSet(TClientDataModel model, IClock clock)
        {
            return new SyncChangeSet()
            {
                LastSync = (model.LastSync ?? Instant.MinValue) < this._lastDeviceSyncTimeFromServer
                    ? model.LastSync ?? Instant.MinValue
                    : this._lastDeviceSyncTimeFromServer,
                SyncStart = clock.GetCurrentInstant(),
                AccountId = this._accountId,
                ClientDeviceId = this._deviceId,
                ClientDeviceName = this._deviceName,
                ChangedCollections = new List<CollectionChangeSet>(),
            };
        }

        private void LoadCollectionChangeSets(SyncChangeSet localChangeSet, List<ICollectionWrapper> localCollections)
        {
            foreach (ICollectionWrapper localCollectionWrapper in localCollections)
            {
                List<IDBDocument> updatedLocalDocs = localCollectionWrapper.ForDBSync.GetUpdatedLocalRows(localChangeSet.LastSync);

                if (updatedLocalDocs.Any())
                {
                    localChangeSet.ChangedCollections.Add(new CollectionChangeSet()
                    {
                        CollectionName = localCollectionWrapper.ForDBSync.CollectionName,
                        ChangedDocuments = updatedLocalDocs,
                    });
                }
            }
        }
    }
}
