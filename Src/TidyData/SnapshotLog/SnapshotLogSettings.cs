 #nullable disable
 using NodaTime;

 namespace TidyData.SnapshotLog
{
    public record SnapshotLogSettings
    {
        private readonly Duration _maxSnapshotAgeToPreserveAll;
        private readonly Duration _maxSnapshotAgeToPreserveOnePerHour;
        private readonly Duration _maxSnapshotAgeToPreserveOnePerDay;
        private readonly string _fileExtension;

        public string SnapshotLogName { get; init; }
        public int MinSnapshotCountBeforeEligibleForDeletion { get; init; }

        public string FileExtension
        {
            get => this._fileExtension;
            init
            {
                this._fileExtension = value ?? ".json";
                if (!this._fileExtension.StartsWith("."))
                    this._fileExtension = "." + this._fileExtension;
            }
        }

        public Duration MaxSnapshotAgeToPreserveAll
        {
            get => Max(this._maxSnapshotAgeToPreserveAll);
            init => this._maxSnapshotAgeToPreserveAll = value;
        }

        public Duration MaxSnapshotAgeToPreserveOnePerHour
        {
            get => Max(this._maxSnapshotAgeToPreserveAll, this._maxSnapshotAgeToPreserveOnePerHour);
            init => this._maxSnapshotAgeToPreserveOnePerHour = value;
        }

        public Duration MaxSnapshotAgeToPreserveOnePerDay
        {
            get => Max(this._maxSnapshotAgeToPreserveAll, this._maxSnapshotAgeToPreserveOnePerHour, this._maxSnapshotAgeToPreserveOnePerDay);
            init => this._maxSnapshotAgeToPreserveOnePerDay = value;
        }

        private static Duration Max(params Duration?[] durations)
        {
            if (durations.Length == 0)
                return Duration.Zero;
            return durations.Select(x => x ?? Duration.Zero).Max();
        }
    }
}
