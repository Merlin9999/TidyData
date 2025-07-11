 #nullable disable
 using System.Globalization;
 using System.Text.RegularExpressions;
 using NodaTime;
 using NodaTime.Text;
 using TidyUtility.Core.Extensions;
 using TidyUtility.Data.Json;

 namespace TidyData.SnapshotLog
{
    public abstract class AbstractSnapshotLog<T> : ISnapshotLog<T>
        where T : new()
    {
        private const string DateTimePattern = "yyyy.MM.dd_HH.mm.ss.fffffff";
        private readonly InstantPattern _dateTimeInstantPattern;
        private readonly IClock _clock;
        private readonly Regex _snapshotNameRegex;

        protected string SnapshotLogName => this.Settings.SnapshotLogName;
        protected ISerializer Serializer { get; }
        protected int CountOfSaves { get; set; } = 0;

        public SnapshotLogSettings Settings { get; }

        protected AbstractSnapshotLog(SnapshotLogSettings settings, ISerializer serializer = null, IClock clock = null)
        {
            this.Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.Serializer = serializer ?? new JsonDotNetSerializer();
            this._clock = clock ?? SystemClock.Instance;
            this._dateTimeInstantPattern = InstantPattern.Create(DateTimePattern, CultureInfo.InvariantCulture);

            string encodedSnapshotLogName = Regex.Escape(this.SnapshotLogName);
            this._snapshotNameRegex = new Regex("^" + encodedSnapshotLogName + @"_[0-9]{4}\.[0-9]{2}\.[0-9]{2}_[0-9]{2}\.[0-9]{2}\.[0-9]{2}\.[0-9]{7}_[0-9]{10}$");
        }

        public virtual async Task<string> SaveSnapshotAsync(T instanceToSave)
        {
            string newSnapshotName = await this.SaveNewSnapshotAsync(instanceToSave);
            await this.DeleteAllEligibleForAutoDeletionAsync();
            return newSnapshotName;
        }

        protected abstract Task<string> SaveNewSnapshotAsync(T instanceToSave);

        public abstract Task<T> LoadSnapshotAsync(string snapshotName);

        public abstract Task<IEnumerable<string>> GetSavedSnapshotNamesAsync();

        public abstract Task DeleteAsync(string snapshotName);

        public abstract Task DeleteAllAsync();

        public abstract Task DeleteAllEligibleForAutoDeletionAsync();

        public virtual async Task<T> LoadLastSavedSnapshotAsync()
        {
            string mostCurrentSnapshot = await this.GetMostCurrentSnapshotAsync();
            if (mostCurrentSnapshot == null)
                return new T();
            return await this.LoadSnapshotAsync(mostCurrentSnapshot);
        }

        public virtual async Task<string> GetMostCurrentSnapshotAsync()
        {
            IEnumerable<string> savedSnapshotNames = await this.GetSavedSnapshotNamesAsync();
            List<string> savedSnapshotNamesAsList = savedSnapshotNames as List<string> ?? savedSnapshotNames.ToList<string>();
            if (savedSnapshotNamesAsList.Any<string>())
                return savedSnapshotNamesAsList.MaxBy<string, string>(x => x.ToLower(CultureInfo.InvariantCulture));
            return null;
        }

        // Used for unit testing.
        internal protected virtual string BuildSnapshotName()
        {
            return $"{this.SnapshotLogName}_{this._dateTimeInstantPattern.Format(this._clock.GetCurrentInstant())}" +
                $"_{this.CountOfSaves++:0000000000}";
        }

        protected virtual bool IsSnapshotNameMatch(string possibleSnapshotName)
        {
            return this._snapshotNameRegex.IsMatch(possibleSnapshotName);
        }

        protected virtual async Task<IEnumerable<string>> GetAllSnapshotNamesEligibleForAutoDeletionAsync()
        {
            var savedSnapshotNames = await this.GetSavedSnapshotNamesAsync();

            return this.FilterToSnapshotNamesEligibleForAutoDeletion(savedSnapshotNames);
        }

        protected virtual IEnumerable<string> FilterToSnapshotNamesEligibleForAutoDeletion(IEnumerable<string> savedSnapshotNames)
        {
            Instant now = this._clock.GetCurrentInstant();
            List<SnapshotNameFilterMetadata> savedSnapshots = savedSnapshotNames
                .Select(snapshotName => new SnapshotNameFilterMetadata()
                {
                    SnapshotName = snapshotName,
                    Age = now - this.ParseDateTimeFromSnapshotName(snapshotName),
                    MarkedForDeletion = false,
                })
                .OrderByDescending(snapshot => snapshot.Age)
                .ToList();

            if (savedSnapshots.IsEmpty())
                return Enumerable.Empty<string>();

            Duration ageOfPrior = Duration.MaxValue;
            Duration oneHour = Duration.FromHours(1);
            Duration oneDay = Duration.FromDays(1);
            foreach (SnapshotNameFilterMetadata snapshot in savedSnapshots)
            {
                if (snapshot.Age > this.Settings.MaxSnapshotAgeToPreserveOnePerDay)
                {
                    snapshot.MarkedForDeletion = true;
                    continue;
                }

                if (snapshot.Age > this.Settings.MaxSnapshotAgeToPreserveOnePerHour)
                {
                    if (ageOfPrior - snapshot.Age >= oneDay)
                        ageOfPrior = snapshot.Age;
                    else
                        snapshot.MarkedForDeletion = true;
                    continue;
                }

                if (snapshot.Age > this.Settings.MaxSnapshotAgeToPreserveAll)
                {
                    if (ageOfPrior - snapshot.Age >= oneHour)
                        ageOfPrior = snapshot.Age;
                    else
                        snapshot.MarkedForDeletion = true;
                    continue;
                }

                break;
            }

            // Ensure that the first snapshot is always kept.
            savedSnapshots[^1].MarkedForDeletion = false;

            int minSnapshotCountBeforeEligibleForDeletion = this.Settings.MinSnapshotCountBeforeEligibleForDeletion;
            if (minSnapshotCountBeforeEligibleForDeletion < 1)
                minSnapshotCountBeforeEligibleForDeletion = 1;

            foreach (SnapshotNameFilterMetadata snapshot in savedSnapshots.TakeLast(minSnapshotCountBeforeEligibleForDeletion))
                snapshot.MarkedForDeletion = false;

            return savedSnapshots.Where(s => s.MarkedForDeletion).Select(s => s.SnapshotName);
        }

        protected virtual Instant ParseDateTimeFromSnapshotName(string snapshotName)
        {
            int startIndex = this.SnapshotLogName.Length + 1;
            int length = snapshotName.Length - startIndex - 11;
            string dateTimeString = snapshotName.Substring(startIndex, length);
            return this._dateTimeInstantPattern.Parse(dateTimeString).GetValueOrThrow();
        }

        private class SnapshotNameFilterMetadata
        {
            public string SnapshotName { get; set; }
            public Duration Age { get; set; }
            public bool MarkedForDeletion { get; set; }
        }
    }
}