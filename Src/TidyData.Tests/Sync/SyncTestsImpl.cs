 #nullable disable
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NodaTime.Testing;
using TidySyncDB.Sync;
using TidySyncDB.UnitTests.TestModel;
using TidySyncDB.UnitTests.TestModel.Cmd;
using TidySyncDB.UnitTests.TestModel.Qry;

namespace TidySyncDB.UnitTests.Sync
{
    //public record GetByIdTestDocumentQuery : GetByIdQuery<TestDocument> { }

    public static class SyncTestsImpl
    {
        public static async Task MainScenariosAsync(
            Database<ClientTestDataModel> device1DB, DBClientSyncExecutor<ClientTestDataModel> device1SyncExecutor,
            Database<ClientTestDataModel> device2DB, DBClientSyncExecutor<ClientTestDataModel> device2SyncExecutor,
            Database<ServerTestDataModel> serverDB)
        {
            //Scenario 1

            var doc01 = new TestDocument() { Desc = "Doc 01" };
            var doc02 = new TestDocument() { Desc = "Doc 02" };
            var doc03 = new TestDocument() { Desc = "Doc 03" };
            var step1ExpectedDocs = new List<TestDocument>() { doc01, doc02, doc03 };

            await device1DB.ExecuteAsync(new InsertTestDocCommand(doc01));
            await device1DB.ExecuteAsync(new InsertTestDocCommand(doc02));
            await device1DB.ExecuteAsync(new InsertTestDocCommand(doc03));

            await device1SyncExecutor.ExecuteAsync();

            List<TestDocument> serverStep1 = await serverDB.ExecuteAsync(new DocGetAllQuery());
            serverStep1.Should().BeEquivalentTo(step1ExpectedDocs);

            List<TestDocument> client1Step1 = await device1DB.ExecuteAsync(new DocGetAllQuery());
            client1Step1.Should().BeEquivalentTo(step1ExpectedDocs);


            //Scenario 2

            var doc04 = new TestDocument() { Desc = "Doc 04" };
            var step2ExpectedDocs = new List<TestDocument>() { doc01, doc02, doc03, doc04 };

            await device2DB.ExecuteAsync(new InsertTestDocCommand(doc04));

            await device2SyncExecutor.ExecuteAsync();
            await device1SyncExecutor.ExecuteAsync();

            List<TestDocument> serverStep2 = await serverDB.ExecuteAsync(new DocGetAllQuery());
            serverStep2.Should().BeEquivalentTo(step2ExpectedDocs);

            List<TestDocument> client1Step2 = await device1DB.ExecuteAsync(new DocGetAllQuery());
            client1Step2.Should().BeEquivalentTo(step2ExpectedDocs);

            List<TestDocument> client2Step2 = await device2DB.ExecuteAsync(new DocGetAllQuery());
            client2Step2.Should().BeEquivalentTo(step2ExpectedDocs);


            // Scenario 3: Delete a row and sync

            var step3ExpectedDocs = new List<TestDocument>() { doc01, doc03, doc04 };
            //var doc01Deleted = new TestDocument(cloneFrom: doc01, meta: doc01.Meta.AsDeleted(true));
            var doc01Deleted = doc02 with { Meta = doc02.Meta.AsDeleted(true) };
            var step3ExpectedSoftDeletedDocs = new List<TestDocument>() { doc01Deleted };

            await device1DB.ExecuteAsync(new SoftDeleteTestDocCommand(doc02.Id));

            await device1SyncExecutor.ExecuteAsync();
            await device2SyncExecutor.ExecuteAsync();

            List<TestDocument> serverStep3 = await serverDB.ExecuteAsync(new DocGetAllQuery() { IncludeDeleted = true });
            serverStep3.Where(x => !x.Meta.Deleted).Should().BeEquivalentTo(step3ExpectedDocs);
            serverStep3.Where(x => x.Meta.Deleted).Should().BeEquivalentTo(step3ExpectedSoftDeletedDocs);
            serverStep3.Single(x => x.Meta.Deleted).Id.Should().Be(doc02.Id);

            List<TestDocument> client1Step3 = await device1DB.ExecuteAsync(new DocGetAllQuery() { IncludeDeleted = true });
            client1Step3.Where(x => !x.Meta.Deleted).Should().BeEquivalentTo(step3ExpectedDocs);
            client1Step3.Where(x => x.Meta.Deleted).Should().BeEquivalentTo(step3ExpectedSoftDeletedDocs);
            client1Step3.Single(x => x.Meta.Deleted).Id.Should().Be(doc02.Id);

            List<TestDocument> client2Step3 = await device2DB.ExecuteAsync(new DocGetAllQuery() { IncludeDeleted = true });
            client2Step3.Where(x => !x.Meta.Deleted).Should().BeEquivalentTo(step3ExpectedDocs);
            client2Step3.Where(x => x.Meta.Deleted).Should().BeEquivalentTo(step3ExpectedSoftDeletedDocs);
            client2Step3.Single(x => x.Meta.Deleted).Id.Should().Be(doc02.Id);


            // Scenario 4: Update a row

            var doc01Updated = doc01 with { Desc = "Doc 03 (Updated)" };
            var step4ExpectedDocs = new List<TestDocument>() { doc03, doc01Updated, doc04 };
            await device1DB.ExecuteAsync(new UpdateTestDocCommand(doc01Updated));

            await device1SyncExecutor.ExecuteAsync();
            await device2SyncExecutor.ExecuteAsync();

            List<TestDocument> serverStep4 = await serverDB.ExecuteAsync(new DocGetAllQuery());
            serverStep4.Should().BeEquivalentTo(step4ExpectedDocs);

            List<TestDocument> client1Step4 = await device1DB.ExecuteAsync(new DocGetAllQuery());
            client1Step4.Should().BeEquivalentTo(step4ExpectedDocs);

            List<TestDocument> client2Step4 = await device2DB.ExecuteAsync(new DocGetAllQuery());
            client2Step4.Should().BeEquivalentTo(step4ExpectedDocs);


            // Scenario 5: Conflicting Update

            var doc04Update1 = doc04 with { Desc = "Doc 04 (Update 1)" };
            var doc04Update2 = doc04 with { Desc = "Doc 04 (Update 2)" };
            var step5ExpectedDocs = new List<TestDocument>() { doc03, doc01Updated, doc04Update2 };

            await device1DB.ExecuteAsync(new UpdateTestDocCommand(doc04Update1));
            await device2DB.ExecuteAsync(new UpdateTestDocCommand(doc04Update2));

            await device2SyncExecutor.ExecuteAsync();
            await device1SyncExecutor.ExecuteAsync(); // Update should be ignored as the version from device2DB is newer

            List<TestDocument> serverStep5 = await serverDB.ExecuteAsync(new DocGetAllQuery());
            serverStep5.Should().BeEquivalentTo(step5ExpectedDocs);

            List<TestDocument> client1Step5 = await device1DB.ExecuteAsync(new DocGetAllQuery());
            client1Step5.Should().BeEquivalentTo(step5ExpectedDocs);

            List<TestDocument> client2Step5 = await device2DB.ExecuteAsync(new DocGetAllQuery());
            client2Step5.Should().BeEquivalentTo(step5ExpectedDocs);

            // Sync back device 2 after Scenario 5 conflict
            await device2SyncExecutor.ExecuteAsync();

            List<TestDocument> serverStep5_2 = await serverDB.ExecuteAsync(new DocGetAllQuery());
            serverStep5_2.Should().BeEquivalentTo(step5ExpectedDocs);

            List<TestDocument> client2Step5_2 = await device2DB.ExecuteAsync(new DocGetAllQuery());
            client2Step5_2.Should().BeEquivalentTo(step5ExpectedDocs);

        }

        public static async Task SoftDeletedDocumentAreInsertedOnSyncAsync(
            Database<ClientTestDataModel> deviceDB, DBClientSyncExecutor<ClientTestDataModel> deviceSyncExecutor, 
            Database<ServerTestDataModel> serverDB)
        {
            var doc01 = new TestDocument() { Desc = "Doc 01" };
            var doc02 = new TestDocument() { Desc = "Doc 02" };
            var doc03 = new TestDocument() { Desc = "Doc 03" };
            var doc04 = new TestDocument() { Desc = "Doc 04" };

            var doc02Deleted = doc02 with { Meta = doc02.Meta.AsDeleted(true) };
            var doc03Deleted = doc03 with { Meta = doc03.Meta.AsDeleted(true) };
            var deviceExpectedDocs = new List<TestDocument>() { doc01, doc02Deleted, doc03Deleted, doc04 };
            var serverExpectedDocs = new List<TestDocument>() { doc01, doc02Deleted, doc03Deleted, doc04 };
            
            await deviceDB.ExecuteAsync(new InsertTestDocCommand(doc01));
            await deviceDB.ExecuteAsync(new InsertTestDocCommand(doc02));
            await deviceDB.ExecuteAsync(new SoftDeleteTestDocCommand(doc02.Id));

            await serverDB.ExecuteAsync(new InsertTestDocCommand(doc03));
            await serverDB.ExecuteAsync(new InsertTestDocCommand(doc04));
            await serverDB.ExecuteAsync(new SoftDeleteTestDocCommand(doc03.Id));

            await deviceSyncExecutor.ExecuteAsync();

            List<TestDocument> deviceDocs = await deviceDB.ExecuteAsync(new DocGetAllQuery() { IncludeDeleted = true });
            deviceDocs.Should().BeEquivalentTo(deviceExpectedDocs);

            List<TestDocument> serverDocs = await serverDB.ExecuteAsync(new DocGetAllQuery() { IncludeDeleted = true });
            serverDocs.Should().BeEquivalentTo(serverExpectedDocs);
        }

        public static async Task PhysicalDeletionOfSoftDeletedWithAgeOlderThanConfigured(
            Database<ClientTestDataModel> deviceDB, DBClientSyncExecutor<ClientTestDataModel> deviceSyncExecutor,
            Database<ServerTestDataModel> serverDB, FakeClock clock)
        {
            var doc01 = new TestDocument() { Desc = "Doc 01" };
            var doc02 = new TestDocument() { Desc = "Doc 02" };
            var doc03 = new TestDocument() { Desc = "Doc 03" };
            var doc03Deleted = doc03 with { Meta = doc03.Meta.AsDeleted(true) };

            var doc04 = new TestDocument() { Desc = "Doc 04" };
            var doc05 = new TestDocument() { Desc = "Doc 05" };
            var doc06 = new TestDocument() { Desc = "Doc 06" };
            var doc06Deleted = doc06 with { Meta = doc06.Meta.AsDeleted(true) };

            var expectedDocs = new List<TestDocument>() { doc01, doc03Deleted, doc04, doc06Deleted };

            await deviceDB.ExecuteAsync(new InsertTestDocCommand(doc01));
            await deviceDB.ExecuteAsync(new InsertTestDocCommand(doc02));
            await deviceDB.ExecuteAsync(new InsertTestDocCommand(doc03));

            await serverDB.ExecuteAsync(new InsertTestDocCommand(doc04));
            await serverDB.ExecuteAsync(new InsertTestDocCommand(doc05));
            await serverDB.ExecuteAsync(new InsertTestDocCommand(doc06));

            // To Be 25 Hours old (24 hours needed to be eligible for deletion based on sync config).
            // At sync time, these SHOULD NOT be synced and these SHOULD be locally physically deleted.
            await deviceDB.ExecuteAsync(new SoftDeleteTestDocCommand(doc02.Id));
            await serverDB.ExecuteAsync(new SoftDeleteTestDocCommand(doc05.Id));

            clock.AdvanceHours(2);

            // To Be 23 Hours old (24 hours needed to be eligible for deletion based on sync config).
            // At sync time, these SHOULD NOT be synced AND these SHOULD NOT be locally physically deleted.
            await deviceDB.ExecuteAsync(new SoftDeleteTestDocCommand(doc03.Id));
            await serverDB.ExecuteAsync(new SoftDeleteTestDocCommand(doc06.Id));

            clock.AdvanceHours(23);

            await deviceSyncExecutor.ExecuteAsync();

            List<TestDocument> deviceDocs = await deviceDB.ExecuteAsync(new DocGetAllQuery() { IncludeDeleted = true });
            deviceDocs.Should().BeEquivalentTo(expectedDocs);

            List<TestDocument> serverDocs = await serverDB.ExecuteAsync(new DocGetAllQuery() { IncludeDeleted = true });
            serverDocs.Should().BeEquivalentTo(expectedDocs);
        }

        public static async Task PhysicalDeletionStoppedIfSoftDeletedDocReferencedByForeignKey(
            Database<ClientTestDataModel> deviceDB, DBClientSyncExecutor<ClientTestDataModel> deviceSyncExecutor,
            Database<ServerTestDataModel> serverDB, FakeClock clock)
        {
            var doc01 = new TestDocument() { Desc = "Doc 01" };
            var doc02 = new TestDocument() { Desc = "Doc 02" };
            var doc03 = new TestDocument() { Desc = "Doc 03" };
            var doc02Deleted = doc02 with { Meta = doc02.Meta.AsDeleted(true) };

            var expectedDocs = new List<TestDocument>() { doc01, doc02Deleted };

            var docWithReferenceDeleted = new TestDocumentWithForeignKey(testDocumentId: doc02.Id, meta: new DocumentMetaData() { Deleted = true });

            await deviceDB.ExecuteAsync(new InsertTestDocCommand(doc01));
            await deviceDB.ExecuteAsync(new InsertTestDocCommand(doc02));
            await deviceDB.ExecuteAsync(new InsertTestDocCommand(doc03));
            
            await deviceDB.ExecuteAsync(new SoftDeleteTestDocCommand(doc02.Id));
            await deviceDB.ExecuteAsync(new SoftDeleteTestDocCommand(doc03.Id));

            clock.AdvanceHours(25);
            await deviceDB.ExecuteAsync(new InsertTestDocWithRefCommand(docWithReferenceDeleted));

            deviceSyncExecutor.ForceMaintCleanup = true;
            await deviceSyncExecutor.ExecuteAsync();

            List<TestDocument> deviceDocs = await deviceDB.ExecuteAsync(new DocGetAllQuery() { IncludeDeleted = true });
            deviceDocs.Should().BeEquivalentTo(expectedDocs);

            List<TestDocument> serverDocs = await serverDB.ExecuteAsync(new DocGetAllQuery() { IncludeDeleted = true });
            serverDocs.Should().BeEquivalentTo(expectedDocs);
        }
    }
}