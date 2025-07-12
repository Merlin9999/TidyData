 #nullable disable
 using FluentAssertions;
 using NodaTime;
 using NodaTime.Testing;
 using TidyData.SnapshotLog;
 using TidyData.Tests.SnapshotLog;

 namespace TidyData.Tests._Shared_Synced
{
    public static class SnapshotLogTestsImpl
    {
        public static async Task LoadEmptySnapshotLogAsync(
            Func<string, ISnapshotLog<ImmutableData>> snapshotLogFactoryMethod)
        {
            const string snapshotLogName = "LoadEmptySnapshotLogAsync";
            await LoadEmptySnapshotLogAsyncImpl(snapshotLogFactoryMethod, snapshotLogName);
        }

        private static async Task LoadEmptySnapshotLogAsyncImpl(
            Func<string, ISnapshotLog<ImmutableData>> snapshotLogFactoryMethod, string snapshotLogName)
        {
            ISnapshotLog<ImmutableData> emptySnapshotLog = snapshotLogFactoryMethod(snapshotLogName);
            ImmutableData readModel = await emptySnapshotLog.LoadLastSavedSnapshotAsync();
            IEnumerable<string> savedSnapshots = await emptySnapshotLog.GetSavedSnapshotNamesAsync();

            readModel.Should().NotBeNull();
            readModel.Name.Should().BeEmpty();
            readModel.Count.Should().Be(0);
            readModel.NestedImmutableObject.Should().BeNull();
            savedSnapshots.Should().BeEmpty();
        }

        public static async Task SaveAndLoadLastSavedImmutableDataToSnapshotAsync(
            Func<string, ISnapshotLog<ImmutableData>> snapshotLogFactoryMethod)
        {
            string snapshotLogName = "SaveAndLoadLastSavedImmutableDataToSnapshotAsync";
            await SaveAndLoadLastSavedImmutableDataToSnapshotAsyncImpl(snapshotLogFactoryMethod, snapshotLogName);
        }

        private static async Task SaveAndLoadLastSavedImmutableDataToSnapshotAsyncImpl(
            Func<string, ISnapshotLog<ImmutableData>> snapshotLogFactoryMethod, string snapshotLogName)
        {
            var origModel = new ImmutableData(name: "Foo", count: 2);
            ISnapshotLog<ImmutableData> snapshotLog1 = snapshotLogFactoryMethod(snapshotLogName);
            await snapshotLog1.DeleteAllAsync();
            await snapshotLog1.SaveSnapshotAsync(origModel);

            var snapshotLog2 = snapshotLogFactoryMethod(snapshotLogName);
            ImmutableData readModel = await snapshotLog2.LoadLastSavedSnapshotAsync();

            readModel.Should().NotBeNull();
            readModel.Name.Should().Be("Foo");
            readModel.Count.Should().Be(2);

            await snapshotLog2.DeleteAllAsync();
        }

        public static async Task SaveAndLoadNamedImmutableDataToSnapshotAsync(
            Func<string, ISnapshotLog<ImmutableData>> snapshotLogFactoryMethod)
        {
            string snapshotLogName = "SaveAndLoadNamedImmutableDataToSnapshotAsync";
            await SaveAndLoadNamedImmutableDataToSnapshotAsyncImpl(snapshotLogFactoryMethod, snapshotLogName);
        }

        private static async Task SaveAndLoadNamedImmutableDataToSnapshotAsyncImpl(
            Func<string, ISnapshotLog<ImmutableData>> snapshotLogFactoryMethod, string snapshotLogName)
        {
            var origModel = new ImmutableData(name: "Foo", count: 2);
            ISnapshotLog<ImmutableData> snapshotLog1 = snapshotLogFactoryMethod(snapshotLogName);
            await snapshotLog1.DeleteAllAsync();
            await snapshotLog1.SaveSnapshotAsync(origModel);

            ISnapshotLog<ImmutableData> snapshotLog2 = snapshotLogFactoryMethod(snapshotLogName);
            List<string> snapshotNames = (await snapshotLog2.GetSavedSnapshotNamesAsync()).ToList();
            ImmutableData readModel = await snapshotLog2.LoadSnapshotAsync(snapshotNames.First());

            readModel.Should().NotBeNull();
            readModel.Name.Should().Be("Foo");
            readModel.Count.Should().Be(2);
            snapshotNames.Should().NotBeNullOrEmpty();
            snapshotNames.First().Should().NotBeNullOrEmpty();

            await snapshotLog2.DeleteAllAsync();
        }

        public static async Task LoadNonExistingNamedSnapshotFromEmptySnapshotLogAsync(
            Func<string, ISnapshotLog<ImmutableData>> snapshotLogFactoryMethod)
        {
            const string snapshotLogName = "LoadNonExistingNamedSnapshotFromEmptySnapshotLogAsync";
            await LoadNonExistingNamedSnapshotFromEmptySnapshotLogAsyncImpl(snapshotLogFactoryMethod, snapshotLogName);
        }

        private static async Task LoadNonExistingNamedSnapshotFromEmptySnapshotLogAsyncImpl(
            Func<string, ISnapshotLog<ImmutableData>> snapshotLogFactoryMethod, string snapshotLogName)
        {
            ISnapshotLog<ImmutableData> emptySnapshotLog = snapshotLogFactoryMethod(snapshotLogName);

            // Call internal method not defined in the ISnapshotLog interface.
            dynamic emptySnapshotLogDynamic = emptySnapshotLog;
            string nonExistingSnapshotName =
                ((Type) emptySnapshotLogDynamic.GetType()).GetMethod("BuildSnapshotName") != null
                    ? emptySnapshotLogDynamic.BuildSnapshotName()
                    : "NonExistingSnapshotName";

            Func<Task> asyncAction = async () => await emptySnapshotLog.LoadSnapshotAsync(nonExistingSnapshotName);
            await asyncAction.Should().ThrowAsync<SnapshotNotFoundException>();
        }

        public static async Task LoadNonExistingNamedSnapshotFromNonEmptySnapshotLogAsync(
            Func<string, ISnapshotLog<ImmutableData>> snapshotLogFactoryMethod)
        {
            const string snapshotLogName = "LoadNonExistingNamedSnapshotFromNonEmptySnapshotLogAsync";
            await LoadNonExistingNamedSnapshotFromNonEmptySnapshotLogAsyncImpl(snapshotLogFactoryMethod,
                snapshotLogName);
        }

        private static async Task LoadNonExistingNamedSnapshotFromNonEmptySnapshotLogAsyncImpl(
            Func<string, ISnapshotLog<ImmutableData>> snapshotLogFactoryMethod, string snapshotLogName)
        {
            ISnapshotLog<ImmutableData> snapshotLog = snapshotLogFactoryMethod(snapshotLogName);
            await snapshotLog.SaveSnapshotAsync(new ImmutableData()); // Make the snapshot set non-empty.

            // Call internal method not defined in the ISnapshotLog interface.
            dynamic snapshotLogDynamic = snapshotLog;
            string nonExistingSnapshotName =
                ((Type) snapshotLogDynamic.GetType()).GetMethod("BuildSnapshotName") != null
                    ? snapshotLogDynamic.BuildSnapshotName()
                    : "NonExistingSnapshotName";


            Func<Task> asyncAction = async () => await snapshotLog.LoadSnapshotAsync(nonExistingSnapshotName);
            await asyncAction.Should().ThrowAsync<SnapshotNotFoundException>();

            await snapshotLog.DeleteAllAsync();
        }

        public static async Task MaxCountInSnapshotLogAsync(
            Func<string, int, ISnapshotLog<ImmutableData>> snapshotLogFactoryMethod)
        {
            string snapshotLogName = "MaxCountInSnapshotLogAsync";
            await MaxCountInSnapshotLogAsyncImpl(snapshotLogFactoryMethod, snapshotLogName);
        }

        private static async Task MaxCountInSnapshotLogAsyncImpl(
            Func<string, int, ISnapshotLog<ImmutableData>> snapshotLogFactoryMethod, string snapshotLogName)
        {
            var model = new ImmutableData(name: "Foo", count: 2);
            int minSnapshotCountBeforeEligibleForDeletion = 5;
            ISnapshotLog<ImmutableData> snapshotLog =
                snapshotLogFactoryMethod(snapshotLogName, minSnapshotCountBeforeEligibleForDeletion);
            var prevNameList = new List<string>();
            await snapshotLog.DeleteAllAsync();
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 0, prevNameList);
            await snapshotLog.SaveSnapshotAsync(model);
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 1, prevNameList);
            await snapshotLog.SaveSnapshotAsync(model);
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 2, prevNameList);
            await snapshotLog.SaveSnapshotAsync(model);
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 3, prevNameList);
            await snapshotLog.SaveSnapshotAsync(model);
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 4, prevNameList);
            await snapshotLog.SaveSnapshotAsync(model);
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 5, prevNameList);
            await snapshotLog.SaveSnapshotAsync(model);
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 5, prevNameList);
            await snapshotLog.SaveSnapshotAsync(model);
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 5, prevNameList);
            await snapshotLog.SaveSnapshotAsync(model);
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 5, prevNameList);
            await snapshotLog.SaveSnapshotAsync(model);
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 5, prevNameList);
            await snapshotLog.SaveSnapshotAsync(model);
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 5, prevNameList);
            await snapshotLog.DeleteAllAsync();
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 0, prevNameList);
        }

        public static async Task MaxAgeInSnapshotLogAsync(
            Func<string, Duration, FakeClock, ISnapshotLog<ImmutableData>> snapshotLogFactoryMethod)
        {
            string snapshotLogName = "MaxAgeInSnapshotLogAsync";
            await MaxAgeInSnapshotLogAsyncImpl(snapshotLogFactoryMethod, snapshotLogName);
        }

        private static async Task MaxAgeInSnapshotLogAsyncImpl(
            Func<string, Duration, FakeClock, ISnapshotLog<ImmutableData>> snapshotLogFactoryMethod,
            string snapshotLogName)
        {
            var now = Instant.FromDateTimeOffset(DateTimeOffset.Now);
            var fakeClock = new FakeClock(now);
            var model = new ImmutableData(name: "Foo", count: 2);
            Duration maxSnapshotAgeToPreserveAll = Duration.FromMinutes(4);
            ISnapshotLog<ImmutableData> snapshotLog =
                snapshotLogFactoryMethod(snapshotLogName, maxSnapshotAgeToPreserveAll, fakeClock);
            var prevNameList = new List<string>();

            await snapshotLog.DeleteAllAsync();
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 0, prevNameList);
            fakeClock.AdvanceSeconds(1);
            await snapshotLog.SaveSnapshotAsync(model);
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 1, prevNameList);
            fakeClock.AdvanceMinutes(1);
            await snapshotLog.SaveSnapshotAsync(model);
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 2, prevNameList);
            fakeClock.AdvanceMinutes(1);
            await snapshotLog.SaveSnapshotAsync(model);
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 3, prevNameList);
            fakeClock.AdvanceMinutes(1);
            await snapshotLog.SaveSnapshotAsync(model);
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 4, prevNameList);
            fakeClock.AdvanceMinutes(1);
            await snapshotLog.SaveSnapshotAsync(model);
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 5, prevNameList);
            fakeClock.AdvanceMinutes(1);
            await snapshotLog.SaveSnapshotAsync(model);
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 5, prevNameList);
            fakeClock.AdvanceMinutes(1);
            await snapshotLog.SaveSnapshotAsync(model);
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 5, prevNameList);
            fakeClock.AdvanceMinutes(2);
            await snapshotLog.SaveSnapshotAsync(model);
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 4, prevNameList);
            fakeClock.AdvanceMinutes(2);
            await snapshotLog.SaveSnapshotAsync(model);
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 3, prevNameList);
            fakeClock.AdvanceMinutes(2);
            await snapshotLog.DeleteAllEligibleForAutoDeletionAsync();
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 2, prevNameList);
            fakeClock.AdvanceMinutes(2);
            await snapshotLog.DeleteAllEligibleForAutoDeletionAsync();
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 1, prevNameList);
            fakeClock.AdvanceMinutes(2);
            await snapshotLog.DeleteAllEligibleForAutoDeletionAsync();
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 1, prevNameList);
        }

        public static async Task MaxAgeAndMaxCountInSnapshotLogAsync(
            Func<string, int, Duration, FakeClock, ISnapshotLog<ImmutableData>> snapshotLogFactoryMethod)
        {
            string snapshotLogName = "MaxAgeAndMaxCountInSnapshotLogAsync";
            await MaxAgeAndMaxCountInSnapshotLogAsyncImpl(snapshotLogFactoryMethod, snapshotLogName);
        }

        private static async Task MaxAgeAndMaxCountInSnapshotLogAsyncImpl(
            Func<string, int, Duration, FakeClock, ISnapshotLog<ImmutableData>> snapshotLogFactoryMethod,
            string snapshotLogName)
        {
            var now = Instant.FromDateTimeOffset(DateTimeOffset.Now);
            var fakeClock = new FakeClock(now);
            var model = new ImmutableData(name: "Foo", count: 2);
            int minSnapshotCountBeforeEligibleForDeletion = 3;
            Duration maxSnapshotAgeToPreserveAll = Duration.FromMinutes(4);
            ISnapshotLog<ImmutableData> snapshotLog = snapshotLogFactoryMethod(snapshotLogName,
                minSnapshotCountBeforeEligibleForDeletion, maxSnapshotAgeToPreserveAll, fakeClock);
            var prevNameList = new List<string>();

            await snapshotLog.DeleteAllAsync();
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 0, prevNameList);
            fakeClock.AdvanceSeconds(1);
            await snapshotLog.SaveSnapshotAsync(model);
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 1, prevNameList);
            fakeClock.AdvanceMinutes(1);
            await snapshotLog.SaveSnapshotAsync(model);
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 2, prevNameList);
            fakeClock.AdvanceMinutes(1);
            await snapshotLog.SaveSnapshotAsync(model);
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 3, prevNameList);
            fakeClock.AdvanceMinutes(1);
            await snapshotLog.SaveSnapshotAsync(model);
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 4, prevNameList);
            fakeClock.AdvanceMinutes(1);
            await snapshotLog.SaveSnapshotAsync(model);
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 5, prevNameList);
            fakeClock.AdvanceMinutes(1);
            await snapshotLog.SaveSnapshotAsync(model);
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 5, prevNameList);
            fakeClock.AdvanceMinutes(1);
            await snapshotLog.SaveSnapshotAsync(model);
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 5, prevNameList);
            fakeClock.AdvanceMinutes(2);
            await snapshotLog.SaveSnapshotAsync(model);
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 4, prevNameList);
            fakeClock.AdvanceMinutes(2);
            await snapshotLog.SaveSnapshotAsync(model);
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 3, prevNameList);
            fakeClock.AdvanceMinutes(2);
            await snapshotLog.DeleteAllEligibleForAutoDeletionAsync();
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 3, prevNameList);
            fakeClock.AdvanceMinutes(2);
            await snapshotLog.DeleteAllEligibleForAutoDeletionAsync();
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 3, prevNameList);
            fakeClock.AdvanceMinutes(2);
            await snapshotLog.DeleteAllEligibleForAutoDeletionAsync();
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 3, prevNameList);
            await snapshotLog.DeleteAllAsync();
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 0, prevNameList);
        }
        
        public static async Task MaxCountAndPreserveByHourAndDayInSnapshotLogAsync(
            Func<string, int, Duration, Duration, Duration, FakeClock, ISnapshotLog<ImmutableData>> snapshotLogFactoryMethod)
        {
            string snapshotLogName = "MaxCountAndPreserveByHourAndDayInSnapshotLogAsync";
            await MaxCountAndPreserveByHourAndDayInSnapshotLogAsyncImpl(snapshotLogFactoryMethod, snapshotLogName);
        }

        private static async Task MaxCountAndPreserveByHourAndDayInSnapshotLogAsyncImpl(
            Func<string, int, Duration, Duration, Duration, FakeClock, ISnapshotLog<ImmutableData>> snapshotLogFactoryMethod,
            string snapshotLogName)
        {
            var now = Instant.FromDateTimeOffset(DateTimeOffset.Now);
            var fakeClock = new FakeClock(now);
            var model = new ImmutableData(name: "Foo", count: 2);
            int minSnapshotCountBeforeEligibleForDeletion = 1;
            Duration maxSnapshotAgeToPreserveAll = Duration.FromHours(12);
            Duration maxSnapshotAgeToPreserveOnePerHour = Duration.FromDays(1);
            Duration maxSnapshotAgeToPreserveOnePerDay = Duration.FromDays(3);
            ISnapshotLog<ImmutableData> snapshotLog = snapshotLogFactoryMethod(snapshotLogName,
                minSnapshotCountBeforeEligibleForDeletion, maxSnapshotAgeToPreserveAll, 
                maxSnapshotAgeToPreserveOnePerHour, maxSnapshotAgeToPreserveOnePerDay, fakeClock);
            var prevNameList = new List<string>();

            await snapshotLog.DeleteAllAsync();
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 0, prevNameList);
            await snapshotLog.SaveSnapshotAsync(model);
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 1, prevNameList);
            fakeClock.AdvanceHours(12);
            await snapshotLog.SaveSnapshotAsync(model);
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 2, prevNameList);
            fakeClock.AdvanceHours(12);
            await snapshotLog.SaveSnapshotAsync(model);
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 3, prevNameList);
            fakeClock.AdvanceHours(12);
            await snapshotLog.SaveSnapshotAsync(model);
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 4, prevNameList);
            fakeClock.AdvanceHours(12);
            await snapshotLog.SaveSnapshotAsync(model);
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 4, prevNameList);
            fakeClock.AdvanceHours(12);
            await snapshotLog.SaveSnapshotAsync(model);
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 5, prevNameList);
            fakeClock.AdvanceHours(12);
            await snapshotLog.SaveSnapshotAsync(model);
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 5, prevNameList);
            fakeClock.AdvanceHours(12);
            await snapshotLog.DeleteAllEligibleForAutoDeletionAsync();
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 4, prevNameList);
            fakeClock.AdvanceHours(12);
            await snapshotLog.DeleteAllEligibleForAutoDeletionAsync();
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 3, prevNameList);
            fakeClock.AdvanceHours(12);
            await snapshotLog.DeleteAllEligibleForAutoDeletionAsync();
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 2, prevNameList);
            fakeClock.AdvanceHours(12);
            await snapshotLog.DeleteAllEligibleForAutoDeletionAsync();
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 2, prevNameList);
            fakeClock.AdvanceHours(12);
            await snapshotLog.DeleteAllEligibleForAutoDeletionAsync();
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 1, prevNameList);
            fakeClock.AdvanceHours(60);
            await snapshotLog.DeleteAllEligibleForAutoDeletionAsync();
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 1, prevNameList);

            await snapshotLog.DeleteAllAsync();
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 0, prevNameList);

            await snapshotLog.SaveSnapshotAsync(model);
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 1, prevNameList);
            fakeClock.AdvanceMinutes(30);
            await snapshotLog.SaveSnapshotAsync(model);
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 2, prevNameList);
            fakeClock.AdvanceMinutes(30);
            await snapshotLog.SaveSnapshotAsync(model);
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 3, prevNameList);
            fakeClock.AdvanceMinutes(30);
            await snapshotLog.SaveSnapshotAsync(model);
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 4, prevNameList);
            fakeClock.AdvanceMinutes(30);
            await snapshotLog.SaveSnapshotAsync(model);
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 5, prevNameList);
            fakeClock.AdvanceHours(10);
            await snapshotLog.DeleteAllEligibleForAutoDeletionAsync();
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 5, prevNameList);

            fakeClock.AdvanceMinutes(30);
            await snapshotLog.DeleteAllEligibleForAutoDeletionAsync();
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 5, prevNameList);
            fakeClock.AdvanceMinutes(30);
            await snapshotLog.DeleteAllEligibleForAutoDeletionAsync();
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 4, prevNameList);
            fakeClock.AdvanceMinutes(30);
            await snapshotLog.DeleteAllEligibleForAutoDeletionAsync();
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 4, prevNameList);
            fakeClock.AdvanceMinutes(30);
            await snapshotLog.DeleteAllEligibleForAutoDeletionAsync();
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 3, prevNameList);
            fakeClock.AdvanceHours(10);
            await snapshotLog.DeleteAllEligibleForAutoDeletionAsync();
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 3, prevNameList);

            fakeClock.AdvanceMinutes(30);
            await snapshotLog.DeleteAllEligibleForAutoDeletionAsync();
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 3, prevNameList);
            fakeClock.AdvanceMinutes(30);
            await snapshotLog.DeleteAllEligibleForAutoDeletionAsync();
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 3, prevNameList);
            fakeClock.AdvanceMinutes(30);
            await snapshotLog.DeleteAllEligibleForAutoDeletionAsync();
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 2, prevNameList);
            fakeClock.AdvanceMinutes(30);
            await snapshotLog.DeleteAllEligibleForAutoDeletionAsync();
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 2, prevNameList);
            fakeClock.AdvanceMinutes(30);
            await snapshotLog.DeleteAllEligibleForAutoDeletionAsync();
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 2, prevNameList);
            fakeClock.AdvanceMinutes(30);
            fakeClock.AdvanceHours(45);
            await snapshotLog.DeleteAllEligibleForAutoDeletionAsync();
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 2, prevNameList);
            fakeClock.AdvanceHours(1);
            await snapshotLog.DeleteAllEligibleForAutoDeletionAsync();
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 1, prevNameList);

            await snapshotLog.DeleteAllAsync();
            prevNameList = await AssertExpectedSnapshotCountAndManagement(snapshotLog, 0, prevNameList);
        }
        
        private static async Task<List<string>> AssertExpectedSnapshotCountAndManagement<T>(ISnapshotLog<T> snapshotLog,
            int expectedSnapshotCount, List<string> prevNameList)
            where T : new()
        {
            List<string> curNameList = (await snapshotLog.GetSavedSnapshotNamesAsync()).OrderByDescending(x => x).ToList();
            curNameList.Count.Should().Be(expectedSnapshotCount);

            IEnumerable<string> addedNames = curNameList.Except(prevNameList);
            IEnumerable<string> deletedNames = prevNameList.Except(curNameList);

            foreach (string addedName in addedNames)
            {
                foreach (string prevNameListItem in prevNameList)
                    prevNameListItem.Should().Match(prevName =>
                        String.Compare(prevName, addedName, StringComparison.InvariantCultureIgnoreCase) < 0);
            }

            if (curNameList.Any())
            {
                foreach (string deletedName in deletedNames)
                {
                    curNameList.First().Should().Match(curName =>
                        String.Compare(curName, deletedName, StringComparison.InvariantCultureIgnoreCase) > 0);
                }
            }

            return curNameList;
        }
    }
}