 #nullable disable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using NodaTime;
using TidySyncDB.Storage;
using TidyTime.Core.DB.Cmd;
using TidyTime.Core.DB.Qry;
using TidyTime.Core.Dto;
using TidyUtility.Serializer;

namespace TidySyncDB.UnitTests.Storage.DBStorage
{
    public static class DBStorageTestImpl
    {
        public static async Task ReadTestImplAsync(
            Func<string, int, Duration?, ISerializer, IClock, IDBStorage<ClientDataModel>> dbStorageFactoryFunc)
        {
            int minSnapshotCountBeforeEligibleForDeletion = 2;
            Duration? maxSnapshotAgeToPreserveAll = Duration.FromMilliseconds(5000);
            var serializer = new SafeJsonDotNetSerializer();

            IDBStorage<ClientDataModel> dbStorage = dbStorageFactoryFunc("ReadSnapshotLog",
                minSnapshotCountBeforeEligibleForDeletion, maxSnapshotAgeToPreserveAll,
                serializer, SystemClock.Instance);
            var db = new Database<ClientDataModel>(dbStorage);

            await db.DeleteDatabaseAsync();

            List<ProjectDto> lists = await db.ExecuteAsync(new ProjectGetAllQuery());

            lists.Should().HaveCount(0);
        }

        public static async Task ReadUpdateTestImplAsync(
            Func<string, int, Duration?, ISerializer, IClock, IDBStorage<ClientDataModel>> dbStorageFactoryFunc)
        {
            int minSnapshotCountBeforeEligibleForDeletion = 2;
            Duration? maxSnapshotAgeToPreserveAll = Duration.FromMilliseconds(5000);
            var serializer = new SafeJsonDotNetSerializer();

            IDBStorage<ClientDataModel> dbStorage = dbStorageFactoryFunc("ReadUpdateSnapshotLog",
                minSnapshotCountBeforeEligibleForDeletion, maxSnapshotAgeToPreserveAll,
                serializer, SystemClock.Instance);

            var db = new Database<ClientDataModel>(dbStorage);

            await db.DeleteDatabaseAsync();

            var getAllQuery = new ProjectGetAllQuery();
            var dto = new ProjectDto() { Name = "World Peace" };
            var insertCommand = new InsertCommand<ProjectDto>() {ToInsert = dto};

            List<ProjectDto> beforeCommand = await db.ExecuteAsync(getAllQuery);
            await db.ExecuteAsync(insertCommand);
            List<ProjectDto> afterCommand = await db.ExecuteAsync(getAllQuery);

            beforeCommand.Should().HaveCount(0);
            afterCommand.Should().HaveCount(1);
        }

        public static async Task ReadUpdateFailsWhenAlreadyLockedImplAsync(bool startWithExistingInitialStorage,
            Func<string, int, Duration?, ISerializer, IClock, IDBStorage<ClientDataModel>> dbStorageFactoryFunc)
        {
            int minSnapshotCountBeforeEligibleForDeletion = 2;
            Duration? maxSnapshotAgeToPreserveAll = Duration.FromMilliseconds(5000);
            var serializer = new SafeJsonDotNetSerializer();

            string snapshotLogName = "OverlappingReadUpdateSnapshotLog" +
                (startWithExistingInitialStorage ? "WithInitialStorage" : "WithNoInitialStorage");

            IDBStorage<ClientDataModel> dbStorage = dbStorageFactoryFunc(snapshotLogName,
                minSnapshotCountBeforeEligibleForDeletion, maxSnapshotAgeToPreserveAll,
                serializer, SystemClock.Instance);

            var db = new Database<ClientDataModel>(dbStorage);
            await db.DeleteDatabaseAsync();

            if (startWithExistingInitialStorage)
                await db.ExecuteAsync(new NullCommand());

            IDBStorage<ClientDataModel> dbStorageToBeNested = dbStorageFactoryFunc(snapshotLogName,
                minSnapshotCountBeforeEligibleForDeletion, maxSnapshotAgeToPreserveAll,
                serializer, SystemClock.Instance);

            var dbToBeNested = new Database<ClientDataModel>(dbStorageToBeNested);
            var commandWithNestedDatabase = new NestedDatabaseTestCommand(dbToBeNested);
            
            Func<Task> func = async () => await db.ExecuteAsync(commandWithNestedDatabase);
            
            await func.Should().ThrowAsync<StorageConcurrencyException>();
        }
    }

    public class NullCommand : ICommand<ClientDataModel>
    {
        public void Execute(ClientDataModel model, CollectionWrapperFactory factory)
        {
            // Do Nothing.
        }
    }


    public class NestedDatabaseTestCommand : ICommand<ClientDataModel>
    {
        private readonly Database<ClientDataModel> _nestedDatabase;

        public NestedDatabaseTestCommand(Database<ClientDataModel> nextedDatabase)
        {
            this._nestedDatabase = nextedDatabase;
        }

        public void Execute(ClientDataModel model, CollectionWrapperFactory factory)
        {
            // This will read the file and write the file back out.
            // Another write based on the previous read MUST FAIL concurrency.
            this._nestedDatabase.ExecuteAsync(new NullCommand()).Wait();
        }
    }
}
