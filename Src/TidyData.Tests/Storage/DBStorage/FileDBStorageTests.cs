 #nullable disable
using NodaTime;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TidyData.SnapshotLog;
using TidyData.Storage;
using TidySyncDB.UnitTests.TestModel;
using TidyUtility.Data.Json;
using Xunit;

namespace TidySyncDB.UnitTests.Storage.DBStorage
{
    public class FileDBStorageTests
    {
        private const string FileExtension = ".json";

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
            await DBStorageTestImpl.ReadUpdateFailsWhenAlreadyLockedImplAsync(false, this.DBStorageFactoryMethod);
        }

        [Fact]
        public async Task ReadUpdateFailsWhenAlreadyLockedWhenInitialStorageDoesNotExist()
        {
            await DBStorageTestImpl.ReadUpdateFailsWhenAlreadyLockedImplAsync(true, this.DBStorageFactoryMethod);
        }

        private IDBStorage<TestDataModel> DBStorageFactoryMethod(string snapshotLogName,
            int minSnapshotCountBeforeEligibleForDeletion, Duration? maxSnapshotAgeToPreserveAll,
            ISerializer serializer, IClock clock)
        {
            var snapshotLogSettings = new SnapshotLogSettings()
            {
                SnapshotLogName = snapshotLogName,
                MinSnapshotCountBeforeEligibleForDeletion = minSnapshotCountBeforeEligibleForDeletion, 
                MaxSnapshotAgeToPreserveAll = maxSnapshotAgeToPreserveAll ?? Duration.Zero, 
                FileExtension = FileExtension,
            };
            return new FileDBStorage<TestDataModel>(snapshotLogSettings, @".\", serializer, clock);
        }
    }
}