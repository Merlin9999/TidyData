 #nullable disable
 using NodaTime;
 using NodaTime.Testing;
 using TidyData.SnapshotLog;
 using TidyData.Tests._Shared_Synced;

 namespace TidyData.Tests.SnapshotLog
{
    public class FileSnapshotLogTests
    {
        private const string FileSetExtension = ".json";
        private readonly string _fileSetRootFolderPath;

        public FileSnapshotLogTests()
        {
            string applicationDataFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string rootFolderPath = Path.Combine(applicationDataFolderPath,
                "SnapshotLog", "TestFiles", "Working", "FileSnapshotLogTests");
            Directory.CreateDirectory(rootFolderPath);
            this._fileSetRootFolderPath = rootFolderPath;
        }

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
            var snapShotLogSettings = new SnapshotLogSettings()
                {
                    SnapshotLogName = snapshotLogName,
                    FileExtension = FileSetExtension,
                };
            
            return new FileSnapshotLog<ImmutableData>(snapShotLogSettings, this._fileSetRootFolderPath);
        }

        private ISnapshotLog<ImmutableData> ConstructSnapshotLogWithMaxCount(string snapshotLogName,
            int minSnapshotCountBeforeEligibleForDeletion)
        {
            var snapShotLogSettings = new SnapshotLogSettings()
            {
                SnapshotLogName = snapshotLogName,
                MinSnapshotCountBeforeEligibleForDeletion = minSnapshotCountBeforeEligibleForDeletion,
                FileExtension = FileSetExtension,
            };

            return new FileSnapshotLog<ImmutableData>(snapShotLogSettings, this._fileSetRootFolderPath);
        }

        private ISnapshotLog<ImmutableData> ConstructSnapshotLogWithMaxAge(string snapshotLogName,
            Duration maxSnapshotAgeToPreserveAll, FakeClock fakeClock)
        {
            var snapShotLogSettings = new SnapshotLogSettings()
            {
                SnapshotLogName = snapshotLogName,
                MaxSnapshotAgeToPreserveAll = maxSnapshotAgeToPreserveAll,
                FileExtension = FileSetExtension,
            };

            return new FileSnapshotLog<ImmutableData>(snapShotLogSettings, this._fileSetRootFolderPath, clock: fakeClock);
        }

        private ISnapshotLog<ImmutableData> ConstructSnapshotLogWithMaxCountAndAge(string snapshotLogName,
            int minSnapshotCountBeforeEligibleForDeletion,
            Duration maxSnapshotAgeToPreserveAll, FakeClock fakeClock)
        {
            var snapShotLogSettings = new SnapshotLogSettings()
            {
                SnapshotLogName = snapshotLogName,
                MinSnapshotCountBeforeEligibleForDeletion = minSnapshotCountBeforeEligibleForDeletion,
                MaxSnapshotAgeToPreserveAll = maxSnapshotAgeToPreserveAll, 
                FileExtension = FileSetExtension,
            };
            return new FileSnapshotLog<ImmutableData>(snapShotLogSettings, this._fileSetRootFolderPath, clock: fakeClock);
        }
    
        private ISnapshotLog<ImmutableData> ConstructSnapshotLogWithMaxCountAndPreserveByHourAndDay(string snapshotLogName,
            int minSnapshotCountBeforeEligibleForDeletion, Duration maxSnapshotAgeToPreserveAll,
            Duration maxSnapshotAgeToPreserveOnePerHour, Duration maxSnapshotAgeToPreserveOnePerDay, FakeClock fakeClock)
        {
            var snapShotLogSettings = new SnapshotLogSettings()
            {
                SnapshotLogName = snapshotLogName,
                MinSnapshotCountBeforeEligibleForDeletion = minSnapshotCountBeforeEligibleForDeletion,
                MaxSnapshotAgeToPreserveAll = maxSnapshotAgeToPreserveAll,
                MaxSnapshotAgeToPreserveOnePerHour = maxSnapshotAgeToPreserveOnePerHour,
                MaxSnapshotAgeToPreserveOnePerDay = maxSnapshotAgeToPreserveOnePerDay,
                FileExtension = FileSetExtension,
            };
            return new FileSnapshotLog<ImmutableData>(snapShotLogSettings, this._fileSetRootFolderPath, clock: fakeClock);
        }
    }
}
