 #nullable disable
 using NodaTime;
 using NodaTime.Testing;
 using TidyData.SnapshotLog;

 namespace TidyData.Tests.SnapshotLog
{
    public class MemorySnapshotLogTests
    {
        [Fact]
        public async Task LoadEmptySnapshotLogAsync()
        {
            await SnapshotLogTestsImpl.LoadEmptySnapshotLogAsync(this.ConstructSnapshotLog);
        }

        [Fact]
        public async Task SaveAndLoadLastSavedImmutableDataToSnapshotAsync()
        {
            await SnapshotLogTestsImpl.SaveAndLoadLastSavedImmutableDataToSnapshotAsync(this.ConstructSnapshotLog);
        }

        [Fact]
        public async Task SaveAndLoadNamedImmutableDataToSnapshotAsync()
        {
            await SnapshotLogTestsImpl.SaveAndLoadNamedImmutableDataToSnapshotAsync(this.ConstructSnapshotLog);
        }

        [Fact]
        public async Task LoadNonExistingNamedSnapshotFromEmptySnapshotLogAsync()
        {
            await SnapshotLogTestsImpl.LoadNonExistingNamedSnapshotFromEmptySnapshotLogAsync(this.ConstructSnapshotLog);
        }

        [Fact]
        public async Task LoadNonExistingNamedSnapshotFromNonEmptySnapshotLogAsync()
        {
            await SnapshotLogTestsImpl.LoadNonExistingNamedSnapshotFromNonEmptySnapshotLogAsync(this.ConstructSnapshotLog);
        }

        [Fact]
        public async Task MaxCountInSnapshotLogAsync()
        {
            await SnapshotLogTestsImpl.MaxCountInSnapshotLogAsync(this.ConstructSnapshotLogWithMaxCount);
        }

        [Fact]
        public async Task MaxAgeInSnapshotLogAsync()
        {
            await SnapshotLogTestsImpl.MaxAgeInSnapshotLogAsync(this.ConstructSnapshotLogWithMaxAge);
        }

        [Fact]
        public async Task MaxAgeAndMaxCountInSnapshotLogAsync()
        {
            await SnapshotLogTestsImpl.MaxAgeAndMaxCountInSnapshotLogAsync(this.ConstructSnapshotLogWithMaxCountAndAge);
        }

        [Fact]
        public async Task MaxCountAndPreserveByHourAndDayInSnapshotLogAsync()
        {
            await SnapshotLogTestsImpl.MaxCountAndPreserveByHourAndDayInSnapshotLogAsync(this.ConstructSnapshotLogWithMaxCountAndPreserveByHourAndDay);
        }

        private ISnapshotLog<ImmutableData> ConstructSnapshotLog(string snapshotLogName)
        {
            return new MemorySnapshotLog<ImmutableData>(new SnapshotLogSettings()
            {
                SnapshotLogName = snapshotLogName,
            });
        }

        private ISnapshotLog<ImmutableData> ConstructSnapshotLogWithMaxCount(string snapshotLogName,
            int minSnapshotCountBeforeEligibleForDeletion)
        {
            return new MemorySnapshotLog<ImmutableData>(new SnapshotLogSettings()
            {
                SnapshotLogName = snapshotLogName,
                MinSnapshotCountBeforeEligibleForDeletion = minSnapshotCountBeforeEligibleForDeletion,
            });
        }

        private ISnapshotLog<ImmutableData> ConstructSnapshotLogWithMaxAge(string snapshotLogName,
            Duration maxSnapshotAgeToPreserveAll, FakeClock fakeClock)
        {
            return new MemorySnapshotLog<ImmutableData>(new SnapshotLogSettings()
            {
                SnapshotLogName = snapshotLogName,
                MaxSnapshotAgeToPreserveAll = maxSnapshotAgeToPreserveAll,
            }, clock: fakeClock);
        }

        private ISnapshotLog<ImmutableData> ConstructSnapshotLogWithMaxCountAndAge(string snapshotLogName,
            int minSnapshotCountBeforeEligibleForDeletion,
            Duration maxSnapshotAgeToPreserveAll, FakeClock fakeClock)
        {
            return new MemorySnapshotLog<ImmutableData>(new SnapshotLogSettings()
            {
                SnapshotLogName = snapshotLogName,
                MinSnapshotCountBeforeEligibleForDeletion = minSnapshotCountBeforeEligibleForDeletion,
                MaxSnapshotAgeToPreserveAll = maxSnapshotAgeToPreserveAll,
            }, clock: fakeClock);
        }

        private ISnapshotLog<ImmutableData> ConstructSnapshotLogWithMaxCountAndPreserveByHourAndDay(string snapshotLogName,
            int minSnapshotCountBeforeEligibleForDeletion, Duration maxSnapshotAgeToPreserveAll, 
            Duration maxSnapshotAgeToPreserveOnePerHour, Duration maxSnapshotAgeToPreserveOnePerDay, FakeClock fakeClock)
        {
            return new MemorySnapshotLog<ImmutableData>(new SnapshotLogSettings()
            {
                SnapshotLogName = snapshotLogName,
                MinSnapshotCountBeforeEligibleForDeletion = minSnapshotCountBeforeEligibleForDeletion,
                MaxSnapshotAgeToPreserveAll = maxSnapshotAgeToPreserveAll,
                MaxSnapshotAgeToPreserveOnePerHour = maxSnapshotAgeToPreserveOnePerHour,
                MaxSnapshotAgeToPreserveOnePerDay = maxSnapshotAgeToPreserveOnePerDay,
            }, clock: fakeClock);
        }
    }
}
