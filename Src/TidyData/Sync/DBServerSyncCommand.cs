 #nullable disable
 using System.Collections.Immutable;
 using NodaTime;
 using TidyUtility.Core.Extensions;

 namespace TidyData.Sync
{
    public class DBServerSyncCommand<TServerDataModel> : IAsyncCommandAndQuery<TServerDataModel, SyncChangeSet>
        where TServerDataModel : IServerDataModel, new()
    {
        private readonly SyncChangeSet _clientSyncChangeSet;
        private readonly DBSyncAlgorithmSettings _dbSyncAlgorithmSettings;
        private TServerDataModel _dataModel;

        public DBServerSyncCommand(SyncChangeSet clientSyncChangeSet, DBSyncAlgorithmSettings dbSyncAlgorithmSettings)
        {
            this._clientSyncChangeSet = clientSyncChangeSet;
            this._dbSyncAlgorithmSettings = dbSyncAlgorithmSettings;
        }

        public async Task<SyncChangeSet> ExecuteAsync(TServerDataModel model, CollectionWrapperFactory factory)
        {
            this._dataModel = model;
            SyncChangeSet localSyncChangeSet = await this.DoSyncAsync(this._clientSyncChangeSet, factory);
            this._dataModel = default;
            return localSyncChangeSet;
        }

        private Task<SyncChangeSet> DoSyncAsync(SyncChangeSet clientChangeSet, CollectionWrapperFactory factory)
        {
            Instant serverSyncStart = factory.Clock.GetCurrentInstant();
            Instant syncStart = this.DetermineSyncStart(clientChangeSet, serverSyncStart);
            Instant lastSync = this.DetermineLastSyncTime(clientChangeSet);

            List<ICollectionWrapper> localCollections = factory.GetCollections(this._dataModel).ToList();
            this.UpdateLocalCollectionsFromRemoteChangesets(localCollections, clientChangeSet, syncStart);
            HashSet<DocumentVersion> clientChangeSetDocumentVersions = clientChangeSet.GetDocumentVersions();
            SyncChangeSet localChangeSet = this.BuildLocalChangeSet(localCollections, lastSync, syncStart, clientChangeSetDocumentVersions);

            this.CleanupEligibleSoftDeletedDocuments(localCollections);

            return Task.FromResult(localChangeSet);
        }

        private Instant DetermineSyncStart(SyncChangeSet clientChangeSet, Instant serverSyncStart)
        {
            Instant syncStart = serverSyncStart < clientChangeSet.SyncStart
                ? serverSyncStart
                : clientChangeSet.SyncStart;
            return syncStart;
        }

        private Instant DetermineLastSyncTime(SyncChangeSet clientChangeSet)
        {
            Instant? lastSync = this._dataModel.RemoteDeviceLookup.TryGetValue(clientChangeSet.ClientDeviceId)?.LastSyncTimeStamp;
            if (lastSync == null)
                lastSync = Instant.MinValue;
            if (clientChangeSet.LastSync.HasValue && clientChangeSet.LastSync < lastSync)
                lastSync = clientChangeSet.LastSync.Value;
            return lastSync.Value;
        }

        private void UpdateLocalCollectionsFromRemoteChangesets(List<ICollectionWrapper> localCollections, 
            SyncChangeSet clientChangeSet, Instant syncStart)
        {
            foreach (CollectionChangeSet remoteChangeSet in clientChangeSet.ChangedCollections)
            {
                ICollectionWrapper localCollectionWrapper =
                    localCollections.Single(x => x.CollectionName == remoteChangeSet.CollectionName);

                localCollectionWrapper.ForDBSync.HandleLocalUpdateFromRemoteDocs(remoteChangeSet.ChangedDocuments);
            }

            ImmutableDictionary<Guid, DeviceInformation> remoteDevices = this._dataModel.RemoteDeviceLookup 
                ?? ImmutableDictionary<Guid, DeviceInformation>.Empty;
            DeviceInformation clientDeviceInfo = remoteDevices.TryGetValue(clientChangeSet.ClientDeviceId)
                ?? new DeviceInformation()
                {
                    Id = clientChangeSet.ClientDeviceId,
                };

            DeviceInformation updatedClientDeviceInfo = clientDeviceInfo with
            {
                Name = string.IsNullOrWhiteSpace(clientChangeSet.ClientDeviceName) 
                    ? "<Unnamed Client>" 
                    : clientChangeSet.ClientDeviceName.Trim(),
                LastSyncTimeStamp = syncStart,
            };

            this._dataModel.RemoteDeviceLookup =
                remoteDevices.SetItem(clientChangeSet.ClientDeviceId, updatedClientDeviceInfo);
        }

        private SyncChangeSet BuildLocalChangeSet(List<ICollectionWrapper> localCollections, Instant lastSync, Instant syncStart, HashSet<DocumentVersion> docVersionsFromRemoteClientToExclude)
        {
            SyncChangeSet localChangeSet = new SyncChangeSet()
            {
                LastSync = lastSync,
                SyncStart = syncStart,
                ChangedCollections = new List<CollectionChangeSet>(),
            };

            foreach (ICollectionWrapper localCollectionWrapper in localCollections)
            {
                List<IDBDocument> updatedLocalDocs = localCollectionWrapper.ForDBSync.GetUpdatedLocalRows(lastSync)
                    .Where(doc => !docVersionsFromRemoteClientToExclude.Contains(doc.Version))
                    .ToList();

                if (updatedLocalDocs.Any())
                {
                    localChangeSet.ChangedCollections.Add(new CollectionChangeSet()
                    {
                        CollectionName = localCollectionWrapper.CollectionName,
                        ChangedDocuments = updatedLocalDocs,
                    });
                }
            }

            return localChangeSet;
        }

        private void CleanupEligibleSoftDeletedDocuments(List<ICollectionWrapper> localCollections)
        {
            List<Instant> deviceSyncTimeStamps = this._dataModel.RemoteDeviceLookup?.Values.Select(x => x.LastSyncTimeStamp).ToList();
            if (deviceSyncTimeStamps == null)
                return;
            if (!deviceSyncTimeStamps.Any())
                return;

            Instant oldestDeviceSyncTimeStamp = deviceSyncTimeStamps.Min();

            Instant deleteTimeStamp =
                oldestDeviceSyncTimeStamp.Minus(this._dbSyncAlgorithmSettings.MinAgeToDeleteSoftDeletedDocs);

            foreach (ICollectionWrapper collection in localCollections)
                collection.ForMaintenance.DeleteSoftDeletedDocsOlderThan(deleteTimeStamp);
        }
    }
}