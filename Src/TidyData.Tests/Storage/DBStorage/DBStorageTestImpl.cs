 #nullable disable
using FluentAssertions;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TidyData;
using TidyData.Storage;
using TidySyncDB.UnitTests.TestModel;
using TidySyncDB.UnitTests.TestModel.Qry;
using TidyUtility.Data.Json;
using TidySyncDB.UnitTests.TestModel.Cmd;

namespace TidySyncDB.UnitTests.Storage.DBStorage
{
    public static class DBStorageTestImpl
    {
        public static async Task ReadTestImplAsync(
            Func<string, int, Duration?, ISerializer, IClock, IDBStorage<TestDataModel>> dbStorageFactoryFunc)
        {
            int minSnapshotCountBeforeEligibleForDeletion = 2;
            Duration? maxSnapshotAgeToPreserveAll = Duration.FromMilliseconds(5000);
            var serializer = new SafeJsonDotNetSerializer();

            IDBStorage<TestDataModel> dbStorage = dbStorageFactoryFunc("ReadSnapshotLog",
                minSnapshotCountBeforeEligibleForDeletion, maxSnapshotAgeToPreserveAll,
                serializer, SystemClock.Instance);
            var db = new Database<TestDataModel>(dbStorage);

            await db.DeleteDatabaseAsync();

            List<TestDocument> list = await db.ExecuteAsync(new DocGetAllQuery());

            list.Should().HaveCount(0);
        }

        public static async Task ReadUpdateTestImplAsync(
            Func<string, int, Duration?, ISerializer, IClock, IDBStorage<TestDataModel>> dbStorageFactoryFunc)
        {
            int minSnapshotCountBeforeEligibleForDeletion = 2;
            Duration? maxSnapshotAgeToPreserveAll = Duration.FromMilliseconds(5000);
            var serializer = new SafeJsonDotNetSerializer();

            IDBStorage<TestDataModel> dbStorage = dbStorageFactoryFunc("ReadUpdateSnapshotLog",
                minSnapshotCountBeforeEligibleForDeletion, maxSnapshotAgeToPreserveAll,
                serializer, SystemClock.Instance);

            var db = new Database<TestDataModel>(dbStorage);

            await db.DeleteDatabaseAsync();

            var getAllQuery = new DocGetAllQuery();
            var dto = new TestDocument() { Desc = "World Peace" };
            var insertCommand = new InsertTestDocCommand(dto);

            List<TestDocument> beforeCommand = await db.ExecuteAsync(getAllQuery);
            await db.ExecuteAsync(insertCommand);
            List<TestDocument> afterCommand = await db.ExecuteAsync(getAllQuery);

            beforeCommand.Should().HaveCount(0);
            afterCommand.Should().HaveCount(1);
        }

        public static async Task ReadUpdateFailsWhenAlreadyLockedImplAsync(bool startWithExistingInitialStorage,
            Func<string, int, Duration?, ISerializer, IClock, IDBStorage<TestDataModel>> dbStorageFactoryFunc)
        {
            int minSnapshotCountBeforeEligibleForDeletion = 2;
            Duration? maxSnapshotAgeToPreserveAll = Duration.FromMilliseconds(5000);
            var serializer = new SafeJsonDotNetSerializer();

            string snapshotLogName = "OverlappingReadUpdateSnapshotLog" +
                (startWithExistingInitialStorage ? "WithInitialStorage" : "WithNoInitialStorage");

            IDBStorage<TestDataModel> dbStorage = dbStorageFactoryFunc(snapshotLogName,
                minSnapshotCountBeforeEligibleForDeletion, maxSnapshotAgeToPreserveAll,
                serializer, SystemClock.Instance);

            var db = new Database<TestDataModel>(dbStorage);
            await db.DeleteDatabaseAsync();

            if (startWithExistingInitialStorage)
                await db.ExecuteAsync(new NullCommand());

            IDBStorage<TestDataModel> dbStorageToBeNested = dbStorageFactoryFunc(snapshotLogName,
                minSnapshotCountBeforeEligibleForDeletion, maxSnapshotAgeToPreserveAll,
                serializer, SystemClock.Instance);

            var dbToBeNested = new Database<TestDataModel>(dbStorageToBeNested);
            var commandWithNestedDatabase = new NestedDatabaseTestCommand(dbToBeNested);
            
            Func<Task> func = async () => await db.ExecuteAsync(commandWithNestedDatabase);
            
            await func.Should().ThrowAsync<StorageConcurrencyException>();
        }
    }

    public class NullCommand : ICommand<TestDataModel>
    {
        public void Execute(TestDataModel model, CollectionWrapperFactory factory)
        {
            // Do Nothing.
        }
    }


    public class NestedDatabaseTestCommand : ICommand<TestDataModel>
    {
        private readonly Database<TestDataModel> _nestedDatabase;

        public NestedDatabaseTestCommand(Database<TestDataModel> nextedDatabase)
        {
            this._nestedDatabase = nextedDatabase;
        }

        public void Execute(TestDataModel model, CollectionWrapperFactory factory)
        {
            // This will read the file and write the file back out.
            // Another write based on the previous read MUST FAIL concurrency.
            this._nestedDatabase.ExecuteAsync(new NullCommand()).Wait();
        }
    }
}
