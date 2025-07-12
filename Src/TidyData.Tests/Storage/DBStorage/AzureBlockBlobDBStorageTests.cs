 #nullable disable
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NodaTime;
using TidySyncDB.Azure.Storage;
using TidySyncDB.Storage;
using TidyTime.Core.DB.Sync.Settings;
using TidyTime.Core.Dto;
using TidyUtility.Azure;
using TidyUtility.Serializer;
using TidyUtility.Storage;
using TidyUtility.UnitTests;
using Xunit;

namespace TidySyncDB.UnitTests.Storage.DBStorage
{
    public class AzureBlockBlobDBStorageTests : IAsyncLifetime
    {
        private readonly string StorageConnectionString = "UseDevelopmentStorage=true";
        private readonly string TestContainerName = "unittests".BuildContainerName();
        private readonly string DBStoragePath = "CreateDBStorage";
        private readonly string BlockBlobExtension = ".json";

        public async Task InitializeAsync()
        {
            await AzureStorageEmulatorManager.EnsureStorageEmulatorIsStartedAsync(TestFolders.AzuriteFolder);
        }

        public Task DisposeAsync() { return Task.CompletedTask; }

        [Fact]
        public async Task Read()
        {
            await DBStorageTestImpl.ReadTestImplAsync(this.DBStorageFactoryMethod);
        }

        [Fact]
        public async Task ReadUpdate()
        {
            await DBStorageTestImpl.ReadUpdateTestImplAsync(this.DBStorageFactoryMethod);
        }

        [Fact]
        public async Task ReadUpdateFailsWhenAlreadyLockedWhenInitialStorageExists()
        {
            await DBStorageTestImpl.ReadUpdateFailsWhenAlreadyLockedImplAsync(true, this.DBStorageFactoryMethod);
        }

        [Fact]
        public async Task ReadUpdateFailsWhenAlreadyLockedWhenInitialStorageDoesNotExist()
        {
            await DBStorageTestImpl.ReadUpdateFailsWhenAlreadyLockedImplAsync(false, this.DBStorageFactoryMethod);
        }

        private IDBStorage<ClientDataModel> DBStorageFactoryMethod(string snapshotLogName,
            int minSnapshotCountBeforeEligibleForDeletion, Duration? maxSnapshotAgeToPreserveAll,
            ISerializer serializer, IClock clock)
        {
            var snapshotLogSettings = new SnapshotLogSettings()
            {
                SnapshotLogName = snapshotLogName,
                MinSnapshotCountBeforeEligibleForDeletion = minSnapshotCountBeforeEligibleForDeletion,
                MaxSnapshotAgeToPreserveAll = maxSnapshotAgeToPreserveAll ?? Duration.Zero, 
                FileExtension = this.BlockBlobExtension,
            };

            return new AzureBlockBlobDBStorage<ClientDataModel>(snapshotLogSettings, StorageConnectionString, TestContainerName, DBStoragePath, serializer, clock);
        }
    }
}
