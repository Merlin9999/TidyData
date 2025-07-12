 #nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NodaTime;
using NodaTime.Testing;
using NodaTime.Text;
using NSubstitute;
using TidyData;
using TidyData.Msg.Notifications;
using TidyData.SnapshotLog;
using TidyData.Storage;
using TidyData.Sync;
using TidyMediator;
using TidySyncDB.UnitTests.Helpers;
using TidySyncDB.UnitTests.TestModel;
using TidySyncDB.UnitTests.TestModel.Cmd;
using TidyUtility.Data.Json;
using Xunit;
using Xunit.Abstractions;

namespace TidySyncDB.UnitTests.Sync
{
    public class SyncTests
    {
        private readonly ISerializer _serializer = new SafeJsonDotNetSerializer();

        private static readonly DBSyncAlgorithmSettings DBSyncAlgorithmSettings = new DBSyncAlgorithmSettings() 
            { MinAgeToDeleteSoftDeletedDocs = Duration.FromDays(30), };

        public SyncTests()
        {
            SyncDBTestHelper.InitDBDocumentEquivalencyDefaults();
        }

        [Fact]
        public void SerializeDeserializeTest()
        {
            //Guid expectedId = Guid.Parse("726766bf-7b9f-4fc5-a922-b2ccc64540a6");
            TestDocument expectedValue = new TestDocument()
            {
                //Id = expectedId,
                Version = new DocumentVersion(),
                Meta = new DocumentMetaData().AsDeleted(),
                Desc = "Test Doc Description",
            };

            string serialized = this._serializer.Serialize(expectedValue);
            TestDocument deserialized = this._serializer.Deserialize<TestDocument>(serialized);

            deserialized.Should().BeEquivalentTo(expectedValue);
            //deserialized.Id.Should().Be(expectedId);
        }

        [Fact]
        public async Task MainScenarios()
        {
            Guid accountId = Guid.NewGuid();
            ServerDBInfo server = await this.CreateServerDBAsync("Server");
            ClientDBInfo device1 = await this.CreateClientDeviceDBAsync("Client1", accountId, false, server.ServerSyncClient);
            ClientDBInfo device2 = await this.CreateClientDeviceDBAsync("Client2", accountId, false, server.ServerSyncClient);

            await SyncTestsImpl.MainScenariosAsync(device1.DB, device1.SyncExecutor, device2.DB, device2.SyncExecutor, server.DB);
            await VerifyServerDeviceIds(accountId, device1, device2);
        }

        [Fact]
        public async Task MainScenariosWithClientCaching()
        {
            Guid accountId = Guid.NewGuid();
            ServerDBInfo server = await this.CreateServerDBAsync("ServerCacheTest");
            ClientDBInfo device1 = await this.CreateClientDeviceDBAsync("Client1CacheTest", accountId, true, server.ServerSyncClient);
            ClientDBInfo device2 = await this.CreateClientDeviceDBAsync("Client2CacheTest", accountId, true, server.ServerSyncClient);

            await SyncTestsImpl.MainScenariosAsync(device1.DB, device1.SyncExecutor, device2.DB, device2.SyncExecutor, server.DB);
            await VerifyServerDeviceIds(accountId, device1, device2);
        }

        private static async Task VerifyServerDeviceIds(Guid accountId, ClientDBInfo device1, ClientDBInfo device2)
        {
            IDBSyncServiceAPI syncServiceAPI = device1.SyncExecutor.GetSyncServiceAPI();
            ListRegisteredDevicesResponse serverDevicesResponse =
                await syncServiceAPI.ListRegisteredDevicesAsync(new ListRegisteredDevicesRequest() { AccountId = accountId });

            serverDevicesResponse.AccountId.Should().Be(accountId);
            serverDevicesResponse.Devices.Should().HaveCount(2);
            serverDevicesResponse.Devices.Should().Contain(device => device.Id == device1.DeviceId);
            serverDevicesResponse.Devices.Should().Contain(device => device.Id == device2.DeviceId);
        }

        [Fact]
        public async Task ClientUpdateNotReturnedBackToSameClient()
        {
            Guid accountId = Guid.NewGuid();
            var device1SyncCompleteNotifications = new List<DBSyncComplete.Event>();
            var device2SyncCompleteNotifications = new List<DBSyncComplete.Event>();

            ServerDBInfo server = await this.CreateServerDBAsync("ServerCacheTest");
            ClientDBInfo device1 = await this.CreateClientDeviceDBAsync("Client1CacheTest", accountId, true, server.ServerSyncClient,
                mediatorSubstituteConfigurator: mediator =>
                {
                    mediator.PublishAsync(Arg.Do<DBSyncComplete.Event>(ev => device1SyncCompleteNotifications.Add(ev)));
                });
            ClientDBInfo device2 = await this.CreateClientDeviceDBAsync("Client2CacheTest", accountId, true, server.ServerSyncClient,
                mediatorSubstituteConfigurator: mediator =>
                {
                    mediator.PublishAsync(Arg.Do<DBSyncComplete.Event>(ev => device2SyncCompleteNotifications.Add(ev)));
                });

            await SyncTestsImpl.MainScenariosAsync(device1.DB, device1.SyncExecutor, device2.DB, device2.SyncExecutor, server.DB);
            
            device1SyncCompleteNotifications.Should().HaveCount(5);
            device1SyncCompleteNotifications[0].IncludedChangesFromServer.Should().BeFalse();
            device1SyncCompleteNotifications[1].IncludedChangesFromServer.Should().BeTrue();
            device1SyncCompleteNotifications[2].IncludedChangesFromServer.Should().BeFalse();
            device1SyncCompleteNotifications[3].IncludedChangesFromServer.Should().BeFalse();
            device1SyncCompleteNotifications[4].IncludedChangesFromServer.Should().BeTrue();

            device2SyncCompleteNotifications.Should().HaveCount(5);
            device2SyncCompleteNotifications[0].IncludedChangesFromServer.Should().BeTrue();
            device2SyncCompleteNotifications[1].IncludedChangesFromServer.Should().BeTrue();
            device2SyncCompleteNotifications[2].IncludedChangesFromServer.Should().BeTrue();
            device2SyncCompleteNotifications[3].IncludedChangesFromServer.Should().BeFalse();
            device2SyncCompleteNotifications[4].IncludedChangesFromServer.Should().BeFalse();
        }

        [Fact]
        public async Task SoftDeletedDocumentsAreInsertedOnSync()
        {
            Guid accountId = Guid.NewGuid();
            ServerDBInfo server = await this.CreateServerDBAsync("ServerInsertSoftDelete");
            ClientDBInfo device1 = await this.CreateClientDeviceDBAsync("Client1InsertSoftDelete", accountId, true, server.ServerSyncClient);

            await SyncTestsImpl.SoftDeletedDocumentAreInsertedOnSyncAsync(device1.DB, device1.SyncExecutor, server.DB);
        }

        [Fact]
        public async Task PhysicalDeletionOfSoftDeletedWithAgeOlderThanConfigured()
        {
            FakeClock clock = new FakeClock(SystemClock.Instance.GetCurrentInstant());
            var dbParams = new DBSyncAlgorithmSettings() { MinAgeToDeleteSoftDeletedDocs = Duration.FromDays(1), };
            Guid accountId = Guid.NewGuid();
            ServerDBInfo server = await this.CreateServerDBAsync("ServerPhysicalDeletion", dbParams, clock);
            ClientDBInfo device1 = await this.CreateClientDeviceDBAsync("ClientPhysicalDeletion", accountId, true, server.ServerSyncClient, dbParams, clock);

            await SyncTestsImpl.PhysicalDeletionOfSoftDeletedWithAgeOlderThanConfigured(device1.DB, device1.SyncExecutor, server.DB, clock);
        }

        [Fact]
        public async Task PhysicalDeletionStoppedIfSoftDeletedDocReferencedByForeignKey()
        {
            FakeClock clock = new FakeClock(SystemClock.Instance.GetCurrentInstant());
            var dbParams = new DBSyncAlgorithmSettings() { MinAgeToDeleteSoftDeletedDocs = Duration.FromDays(1) };
            Guid accountId = Guid.NewGuid();
            ServerDBInfo server = await this.CreateServerDBAsync("ServerPhysicalDeletionStopped", dbParams, clock);
            ClientDBInfo device1 = await this.CreateClientDeviceDBAsync("ClientPhysicalDeletionStopped", accountId, true, server.ServerSyncClient, dbParams, clock);

            await SyncTestsImpl.PhysicalDeletionStoppedIfSoftDeletedDocReferencedByForeignKey(device1.DB, device1.SyncExecutor, server.DB, clock);
        }

        private async Task<ServerDBInfo> CreateServerDBAsync(string snapshotLogName, DBSyncAlgorithmSettings dbSyncAlgorithmSettings = null, IClock clock = null)
        {
            IDBStorage<ServerTestDataModel> dbStorage = new MemoryDBStorage<ServerTestDataModel>(new SnapshotLogSettings()
            {
                SnapshotLogName = snapshotLogName,
                MinSnapshotCountBeforeEligibleForDeletion = 2,
                MaxSnapshotAgeToPreserveAll = Duration.FromMilliseconds(50),
            }, this._serializer);
            var db = new Database<ServerTestDataModel>(dbStorage, clock: clock);
            await db.DeleteDatabaseAsync();

            var serverSyncClient = new LocalDBServerClientSync<ServerTestDataModel>(db,
                clientChangeSet => new DBServerSyncCommand<ServerTestDataModel>(clientChangeSet, dbSyncAlgorithmSettings ?? DBSyncAlgorithmSettings));

            return new ServerDBInfo()
            {
                DB = db,
                ServerSyncClient = serverSyncClient,
            };
        }

        private async Task<ClientDBInfo> CreateClientDeviceDBAsync(string snapshotLogName, Guid accountId, bool supportQueryCaching,
            LocalDBServerClientSync<ServerTestDataModel> serverSyncClient, DBSyncAlgorithmSettings dbSyncAlgorithmSettings = null, IClock clock = null, 
            Action<IMediator> mediatorSubstituteConfigurator = null)
        {
            IMediator mediator = Substitute.For<IMediator>();
            if (mediatorSubstituteConfigurator != null)
                mediatorSubstituteConfigurator(mediator);

            IDBStorage<ClientTestDataModel> dbStorage = new MemoryDBStorage<ClientTestDataModel>(new SnapshotLogSettings()
            {
                SnapshotLogName = snapshotLogName,
                MinSnapshotCountBeforeEligibleForDeletion = 2,
                MaxSnapshotAgeToPreserveAll = Duration.FromMilliseconds(50),
            }, this._serializer);
            if (supportQueryCaching)
                dbStorage = new CacheDBStorageAdapter<ClientTestDataModel>(dbStorage);

            var db = new Database<ClientTestDataModel>(dbStorage, clock: clock);
            await db.DeleteDatabaseAsync();

            Guid deviceId = Guid.NewGuid();
            string deviceName = "Client Device";
            var syncExecutor = new DBClientSyncExecutor<ClientTestDataModel>(db, serverSyncClient, accountId, deviceId, deviceName, 
                dbSyncAlgorithmSettings ?? DBSyncAlgorithmSettings, mediator);

            return new ClientDBInfo()
            {
                DeviceId = deviceId,
                DB = db,
                SyncExecutor = syncExecutor,
            };
        }
    }

    public class ServerDBInfo
    {
        public Database<ServerTestDataModel> DB { get; set; }
        public LocalDBServerClientSync<ServerTestDataModel> ServerSyncClient { get; set; }
    }

    public class ClientDBInfo
    {
        public Guid DeviceId { get; set; }
        public Database<ClientTestDataModel> DB { get; set; }
        public DBClientSyncExecutor<ClientTestDataModel> SyncExecutor { get; set; }
    }
}