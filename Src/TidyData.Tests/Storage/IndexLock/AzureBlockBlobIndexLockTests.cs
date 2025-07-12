 #nullable disable
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using NodaTime;
using TidySyncDB.Azure.Storage;
using TidySyncDB.Storage;
using TidyTime.Core.DB.Sync.Settings;
using TidyUtility.Azure;
using TidyUtility.Serializer;
using TidyUtility.UnitTests;
using Xunit;

namespace TidySyncDB.UnitTests.Storage.IndexLock
{
    public class AzureBlockBlobIndexLockTests : IAsyncLifetime
    {
        private readonly string BlobFolderPath = "AzureBlockBlobIndexLockTests";
        private readonly string StorageConnectionString = "UseDevelopmentStorage=true";
        private readonly string TestContainerName = "unittests".BuildContainerName();

        public async Task InitializeAsync()
        {
            await AzureStorageEmulatorManager.EnsureStorageEmulatorIsStartedAsync(TestFolders.AzuriteFolder);
        }

        public Task DisposeAsync() { return Task.CompletedTask; }

        [Fact]
        public async Task LockReadUpdateUnlockLockReadUnlock()
        {
            string fileName = Path.Combine(BlobFolderPath, "LockReadUpdateUnlockLockReadUnlock.json");
            IIndexLock indexLock = new AzureBlockBlobIndexLock(StorageConnectionString, TestContainerName, fileName, new JsonDotNetSerializer(), SystemClock.Instance);
            await IndexLockTestsImpl.LockReadUpdateUnlockLockReadUnlockImplAsync(indexLock);
        }

        [Fact]
        public async Task ReadWriteFailsWhenAlreadyLocked()
        {
            string fileName = Path.Combine(BlobFolderPath, "ReadWriteFailsWhenAlreadyLocked.json");
            IIndexLock indexLock1 = new AzureBlockBlobIndexLock(StorageConnectionString, TestContainerName, fileName, new JsonDotNetSerializer(), SystemClock.Instance);
            IIndexLock indexLock2 = new AzureBlockBlobIndexLock(StorageConnectionString, TestContainerName, fileName, new JsonDotNetSerializer(), SystemClock.Instance);
            await IndexLockTestsImpl.ReadWriteFailsWhenAlreadyLockedImplAsync(indexLock1, indexLock2);
        }

        [Fact]
        public async Task WriteBeforeReadFail()
        {
            string fileName = Path.Combine(BlobFolderPath, "WriteBeforeReadFail.json");
            IIndexLock indexLock = new AzureBlockBlobIndexLock(StorageConnectionString, TestContainerName, fileName, new JsonDotNetSerializer(), SystemClock.Instance);
            await IndexLockTestsImpl.WriteBeforeReadFailImplAsync(indexLock);
        }

    }
}