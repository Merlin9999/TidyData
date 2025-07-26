 #nullable disable
 using System.Collections.Immutable;
 using System.Reflection;
 using NodaTime;
 using TidyData.Query;
 using TidyUtility.Core.Extensions;
 using TidyUtility.Data.Json;

 namespace TidyData.Sync
{
    /// <summary>
    /// The primary purpose of this class is testing. Unit tests need to simulate the serialization of data to
    /// find serialization related issues. Note: This class may also be used when running locally to avoid
    /// running server code in Azure all the time.
    /// </summary>
    /// <typeparam name="TServerDataModel"></typeparam>
    public class LocalDBServerClientSync<TServerDataModel> : IDBClientSync
        where TServerDataModel : class, ISyncServerDataModel, new()
    {
        private readonly Database<TServerDataModel> _serverDatabase;
        private readonly Func<SyncChangeSet, DBServerSyncCommand<TServerDataModel>> _dbSyncServerCommandFactoryFunc;
        private readonly ISerializer _serializer = new SafeJsonDotNetSerializer();

        public LocalDBServerClientSync(Database<TServerDataModel> serverDatabase, Func<SyncChangeSet, DBServerSyncCommand<TServerDataModel>> dbSyncServerCommandFactoryFunc)
        {
            this._serverDatabase = serverDatabase;
            this._dbSyncServerCommandFactoryFunc = dbSyncServerCommandFactoryFunc;
        }

        public Task<GetSyncServiceStatusResponse> GetSyncServiceStatus(GetSyncServiceStatusRequest request)
        {
            AssemblyVersionInfo versionInfo = Assembly.GetExecutingAssembly().GetVersionInformation();

            return Task.FromResult(new GetSyncServiceStatusResponse()
            {
                IsAvailable = true,
                Version = versionInfo.Version,
                InformationalVersion = versionInfo.InformationalVersion,
            });
        }

        public async Task<GetLastDeviceSyncTimeResponse> GetLastDeviceSyncTimeAsync(GetLastDeviceSyncTimeRequest request)
        {
            var query = new DeviceGetLastSyncTimeQuery<TServerDataModel>(request.DeviceId);
            Instant lastSyncTime = await this._serverDatabase.ExecuteAsync(query);

            return new GetLastDeviceSyncTimeResponse()
            {
                LastDeviceSyncTime = lastSyncTime,
            };
        }

        public async Task<SynchronizeResponse> SynchronizeAsync(SynchronizeRequest request)
        {
            // Serialize and deserialize the request and response to simulate what would happen to the data with communication
            // over HTTPS. This makes for a better test of the system. Also allows the client and the server data model to
            // vary as long as they remain compatible. To remain compatible, they must have the same collections (related by
            // inheritance), the server must derive from IDataModel, and the client must also deriving from ISyncClientDataModel.

            string serializedRequest = this._serializer.Serialize(request);
            SynchronizeRequest deserializedRequest = this._serializer.Deserialize<SynchronizeRequest>(serializedRequest);
            
            DBServerSyncCommand<TServerDataModel> dbServerSyncCommand = this._dbSyncServerCommandFactoryFunc(deserializedRequest.ClientChangeSet);
            SyncChangeSet serverChangeSet =  await this._serverDatabase.ExecuteAsync(dbServerSyncCommand);
            return new SynchronizeResponse()
            {
                ServerChangeSet = serverChangeSet,
            };
        }

        public async Task<ListRegisteredDevicesResponse> ListRegisteredDevicesAsync(ListRegisteredDevicesRequest request)
        {
            var query = new ClientDeviceInformationQuery<TServerDataModel>();
            ImmutableList<DeviceInformation> dbSyncDevices =  await this._serverDatabase.ExecuteAsync(query);

            return new ListRegisteredDevicesResponse()
            {
                AccountId = request.AccountId,
                Devices = dbSyncDevices,
            };
        }

        public async Task<DeleteRegisteredDeviceResponse> DeleteRegisteredDeviceAsync(DeleteRegisteredDeviceRequest request)
        {
            var command = new ClientDeviceDeleteCommand<TServerDataModel>(request.DeviceId);
            await this._serverDatabase.ExecuteAsync(command);

            return new DeleteRegisteredDeviceResponse()
            {
                Success = true,
            };
        }
    }
}