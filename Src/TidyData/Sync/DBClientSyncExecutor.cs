 #nullable disable
 using Polly;
 using Polly.Retry;
 using System.Reflection;
 using TidyData.Maint;
 using TidyData.Msg.Notifications;
 using TidyMediator;
 using TidyUtility.Core.Extensions;

 namespace TidyData.Sync
{
    public interface IDBClientSyncExecutor : IDBLocalCleanupExecutor
    {
        bool ForceMaintCleanup { get; set; }
        IDBSyncServiceAPI GetSyncServiceAPI();
    }

    public class DBClientSyncExecutor
    {
        protected static readonly AsyncRetryPolicy DBHttpStatusErrorRetryPolicy = Policy.Handle<DBSyncHttpStatusErrorException>()
            // Exponential Back Off Retry Policy - Retry after 0.2, 0.4, 0.8, 1.6, 3.2, & 6.4 seconds
            .WaitAndRetryAsync(6, attempt => TimeSpan.FromSeconds(0.1 * Math.Pow(2, attempt)));
    }

    public class DBClientSyncExecutor<TClientDataModel> : DBClientSyncExecutor, IDBClientSyncExecutor
        where TClientDataModel : ISyncClientDataModel, new()
    {
        private static volatile AssemblyVersionInfo _assemblyVersionInfoCache;

        private readonly IDatabase<TClientDataModel> _database;
        private readonly IDBClientSync _dbClientSync;
        private readonly Guid _accountId;
        private readonly Guid _deviceId;
        private readonly string _deviceName;
        private readonly DBSyncAlgorithmSettings _dbSyncAlgorithmSettings;
        private readonly IMediator _mediator;

        public DBClientSyncExecutor(IDatabase<TClientDataModel> database, IDBClientSync dbClientSync, Guid accountId,
            Guid deviceId, string deviceName, DBSyncAlgorithmSettings dbSyncAlgorithmSettings, IMediator mediator)
        {
            this._database = database;
            this._dbClientSync = dbClientSync;
            this._accountId = accountId;
            this._deviceId = deviceId;
            this._deviceName = deviceName;
            this._dbSyncAlgorithmSettings = dbSyncAlgorithmSettings;
            this._mediator = mediator;
        }

        public bool ForceMaintCleanup { get; set; }

        public async Task ExecuteAsync()
        {
            await this.EnsureDBSyncServiceOfMatchingVersionIsAvailable();

            try
            {
                var request = new GetLastDeviceSyncTimeRequest()
                {
                    AccountId = this._accountId,
                    DeviceId = this._deviceId,
                };
                GetLastDeviceSyncTimeResponse response = await this._dbClientSync.GetLastDeviceSyncTimeAsync(request);

                var clientChangeSetQuery = new DBClientSyncGetLocalChangesQuery<TClientDataModel>(
                    response.LastDeviceSyncTime,
                    this._accountId, this._deviceId, this._deviceName);
                SyncChangeSet clientChangeSet = await this._database.ExecuteAsync(clientChangeSetQuery);

                var syncRequest = new SynchronizeRequest() { ClientChangeSet = clientChangeSet, };
                SynchronizeResponse synchronizeResponse = await this._dbClientSync.SynchronizeAsync(syncRequest);

                // Avoid saving a snapshot with no changes other than the Sync Start.
                if (this.ForceMaintCleanup || synchronizeResponse.ServerChangeSet.ChangedCollections.Any())
                {
                    var clientApplyServerChangesCommand = new DBClientSyncApplyServerChangesCommand<TClientDataModel>(
                        synchronizeResponse.ServerChangeSet, clientChangeSet.SyncStart, this._dbSyncAlgorithmSettings);

                    await this._database.ExecuteAsync(clientApplyServerChangesCommand);
                }

                await this._mediator.PublishAsync(new DBSyncComplete.Event()
                {
                    AccountId = this._accountId,
                    IncludedChangesFromServer = synchronizeResponse.ServerChangeSet.ChangedCollections.Any(),
                });
            }
            catch (DBSyncHttpStatusErrorException se)
            {
                throw new DBSyncUnavailableException(se);
            }
            catch (Exception e)
            {
                throw new DBSyncUnavailableException(e);
            }
        }

        public IDBSyncServiceAPI GetSyncServiceAPI()
        {
            return this._dbClientSync;
        }

        private async Task EnsureDBSyncServiceOfMatchingVersionIsAvailable()
        {
            GetSyncServiceStatusResponse statusResponse;
            try
            {
                var statusRequest = new GetSyncServiceStatusRequest();

                statusResponse = await DBHttpStatusErrorRetryPolicy
                    .ExecuteAsync(async token => await this._dbClientSync.GetSyncServiceStatus(statusRequest), CancellationToken.None);
            }
            catch (DBSyncHttpStatusErrorException se)
            {
                throw new DBSyncUnavailableException(se);
            }
            catch (Exception e)
            {
                throw new DBSyncUnavailableException(e);
            }

            if (!statusResponse.IsAvailable)
                throw new DBSyncUnavailableException();

            AssemblyVersionInfo versionInfo = GetAssemblyVersionInfo();

            if (versionInfo.Version != statusResponse.Version)
                throw new DBSyncVersionMismatchException(versionInfo.Version, statusResponse.Version);
        }

        private static AssemblyVersionInfo GetAssemblyVersionInfo()
        {
            if (_assemblyVersionInfoCache == null)
                _assemblyVersionInfoCache = Assembly.GetExecutingAssembly().GetVersionInformation();

            return _assemblyVersionInfoCache;
        }
    }
}