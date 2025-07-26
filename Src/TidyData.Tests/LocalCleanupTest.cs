

using FluentAssertions;
using NodaTime;
using NodaTime.Testing;
using TidyData.SnapshotLog;
using TidyData.Storage;
using TidyData.Sync;
using TidyData.Tests._Shared_Synced.TestModel;
using TidyData.Tests._Shared_Synced.TestModel.Cmd;
using TidyData.Tests._Shared_Synced.TestModel.Qry;
using TidyData.Tests.Sync;
using TidyUtility.Data.Json;

namespace TidyData.Tests;

public class LocalCleanupTest
{
    private readonly ISerializer _serializer = new SafeJsonDotNetSerializer();

    [Fact]
    public async Task PhysicalDeletionOfSoftDeletedWithAgeOlderThanConfigured()
    {
        FakeClock fakeClock = new FakeClock(SystemClock.Instance.GetCurrentInstant());
        var dbParams = new DBLocalAlgorithmSettings() { MinAgeToDeleteSoftDeletedDocs = Duration.FromDays(1), };
        Guid accountId = Guid.NewGuid();
        var dbInfo = await CreateDBInfoAsync("LocalDBPhysicalDeletionOfSoftDeleted", accountId, true, dbParams, fakeClock);

        await PhysicalDeletionOfSoftDeletedWithAgeOlderThanConfiguredImpl(dbInfo.DB, dbInfo.CleanupExecutor, fakeClock);
    }

    [Fact]
    public async Task PhysicalDeletionStoppedIfSoftDeletedDocReferencedByForeignKey()
    {
        FakeClock fakeClock = new FakeClock(SystemClock.Instance.GetCurrentInstant());
        var dbParams = new DBLocalAlgorithmSettings() { MinAgeToDeleteSoftDeletedDocs = Duration.FromDays(1), };
        Guid accountId = Guid.NewGuid();
        var dbInfo = await CreateDBInfoAsync("LocalDBPhysicalDeletionOfSoftDeleted", accountId, true, dbParams, fakeClock);

        await PhysicalDeletionStoppedIfSoftDeletedDocReferencedByForeignKeyImpl(dbInfo.DB, dbInfo.CleanupExecutor, fakeClock);
    }

    private static async Task PhysicalDeletionOfSoftDeletedWithAgeOlderThanConfiguredImpl(
        Database<TestDataModel> localDB, DBLocalCleanupExecutor<TestDataModel> localCleanupExecutor,
        FakeClock clock)
    {
        var doc01 = new TestDocument() { Desc = "Doc 01" };
        var doc02 = new TestDocument() { Desc = "Doc 02" };
        var doc03 = new TestDocument() { Desc = "Doc 03" };
        var doc03Deleted = doc03 with { Meta = doc03.Meta.AsDeleted(true) };

        var expectedDocs = new List<TestDocument>() { doc01, doc03Deleted };

        await localDB.ExecuteAsync(new InsertTestDocCommand(doc01));
        await localDB.ExecuteAsync(new InsertTestDocCommand(doc02));
        await localDB.ExecuteAsync(new InsertTestDocCommand(doc03));

        // To Be 25 Hours old (24 hours needed to be eligible for deletion based on sync config).
        // At sync time, these SHOULD NOT be synced and these SHOULD be locally physically deleted.
        await localDB.ExecuteAsync(new SoftDeleteTestDocCommand(doc02.Id));

        clock.AdvanceHours(2);

        // To Be 23 Hours old (24 hours needed to be eligible for deletion based on sync config).
        // At sync time, these SHOULD NOT be synced AND these SHOULD NOT be locally physically deleted.
        await localDB.ExecuteAsync(new SoftDeleteTestDocCommand(doc03.Id));

        clock.AdvanceHours(23);

        await localCleanupExecutor.ExecuteAsync();

        List<TestDocument> localDocs = await localDB.ExecuteAsync(new DocGetAllQuery() { IncludeDeleted = true });
        localDocs.Should().BeEquivalentTo(expectedDocs);
    }

    private static async Task PhysicalDeletionStoppedIfSoftDeletedDocReferencedByForeignKeyImpl(
        Database<TestDataModel> localDB, DBLocalCleanupExecutor<TestDataModel> localCleanupExecutor,
        FakeClock clock)
    {
        var doc01 = new TestDocument() { Desc = "Doc 01" };
        var doc02 = new TestDocument() { Desc = "Doc 02" };
        var doc03 = new TestDocument() { Desc = "Doc 03" };
        var doc02Deleted = doc02 with { Meta = doc02.Meta.AsDeleted(true) };

        var expectedDocs = new List<TestDocument>() { doc01, doc02Deleted };

        var docWithReferenceDeleted = new TestDocumentWithForeignKey(testDocumentId: doc02.Id, meta: new DocumentMetaData() { Deleted = true });

        await localDB.ExecuteAsync(new InsertTestDocCommand(doc01));
        await localDB.ExecuteAsync(new InsertTestDocCommand(doc02));
        await localDB.ExecuteAsync(new InsertTestDocCommand(doc03));

        await localDB.ExecuteAsync(new SoftDeleteTestDocCommand(doc02.Id));
        await localDB.ExecuteAsync(new SoftDeleteTestDocCommand(doc03.Id));

        clock.AdvanceHours(25);
        await localDB.ExecuteAsync(new InsertTestDocWithRefCommand(docWithReferenceDeleted));

        await localCleanupExecutor.ExecuteAsync();

        List<TestDocument> localDocs = await localDB.ExecuteAsync(new DocGetAllQuery() { IncludeDeleted = true });
        localDocs.Should().BeEquivalentTo(expectedDocs);
    }

    private async Task<TestDBInfo> CreateDBInfoAsync(string snapshotLogName, Guid accountId, bool supportQueryCaching,
        DBLocalAlgorithmSettings dbParams = null, IClock clock = null)
    {
        IDBStorage<TestDataModel> dbStorage = new MemoryDBStorage<TestDataModel>(new SnapshotLogSettings()
        {
            SnapshotLogName = snapshotLogName,
            MinSnapshotCountBeforeEligibleForDeletion = 2,
            MaxSnapshotAgeToPreserveAll = Duration.FromMilliseconds(50),
        }, this._serializer);
        if (supportQueryCaching)
            dbStorage = new CacheDBStorageAdapter<TestDataModel>(dbStorage);

        var db = new Database<TestDataModel>(dbStorage, clock: clock);
        await db.DeleteDatabaseAsync();

        var cleanupExecutor = new DBLocalCleanupExecutor<TestDataModel>(db, dbParams, clock);

        return new TestDBInfo()
        {
            DB = db,
            CleanupExecutor = cleanupExecutor,
        };
    }
}

public class TestDBInfo
{
    public Database<TestDataModel> DB { get; set; }
    public DBLocalCleanupExecutor<TestDataModel> CleanupExecutor { get; set; }
}

