 #nullable disable
 using NodaTime;

 namespace TidyData.Sync
{
    public class DBClientSyncApplyServerChangesCommand<TClientDataModel> : ICommand<TClientDataModel>, IClientSyncCommand
        where TClientDataModel : IClientDataModel, new()
    {
        private readonly SyncChangeSet _serverChangeSet;
        private readonly Instant _syncStart;
        private readonly DBSyncAlgorithmSettings _dbSyncAlgorithmSettings;

        public DBClientSyncApplyServerChangesCommand(SyncChangeSet serverChangeSet, Instant syncStart, DBSyncAlgorithmSettings dbSyncAlgorithmSettings)
        {
            this._serverChangeSet = serverChangeSet;
            this._syncStart = syncStart;
            this._dbSyncAlgorithmSettings = dbSyncAlgorithmSettings;
        }

        public void Execute(TClientDataModel model, CollectionWrapperFactory factory)
        {
            List<ICollectionWrapper> localClientCollections = factory.GetCollections(model).ToList();

            UpdateLocalCollectionsFromServerChanges(model, this._serverChangeSet, localClientCollections, this._syncStart);
            CleanupEligibleSoftDeletedDocuments(model, localClientCollections, this._dbSyncAlgorithmSettings);
        }

        private static void UpdateLocalCollectionsFromServerChanges(TClientDataModel model, SyncChangeSet serverChangeSet,
            List<ICollectionWrapper> localCollections, Instant syncStart)
        {
            foreach (CollectionChangeSet serverCollectionChangeSet in serverChangeSet.ChangedCollections)
            {
                ICollectionWrapper localCollectionWrapper =
                    localCollections.Single(x => x.CollectionName == serverCollectionChangeSet.CollectionName);

                localCollectionWrapper.ForDBSync
                    .HandleLocalUpdateFromRemoteDocs(serverCollectionChangeSet.ChangedDocuments);
            }

            model.LastSync = syncStart;
        }

        private static void CleanupEligibleSoftDeletedDocuments(TClientDataModel model,
            List<ICollectionWrapper> localCollections, DBSyncAlgorithmSettings dbSyncAlgorithmSettings)
        {
            Instant deleteTimeStamp =
                model.LastSync.Value.Minus(dbSyncAlgorithmSettings.MinAgeToDeleteSoftDeletedDocs);

            foreach (ICollectionWrapper collection in localCollections)
                collection.ForMaintenance.DeleteSoftDeletedDocsOlderThan(deleteTimeStamp);
        }
    }
}
