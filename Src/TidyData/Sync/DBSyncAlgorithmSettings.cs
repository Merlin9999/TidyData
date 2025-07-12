 #nullable disable
 using NodaTime;

 namespace TidyData.Sync
{
    public record DBSyncAlgorithmSettings
    {
        public Duration MinAgeToDeleteSoftDeletedDocs { get; init; }
    }
}
