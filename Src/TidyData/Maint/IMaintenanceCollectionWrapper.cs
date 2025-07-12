 #nullable disable
 using NodaTime;

 namespace TidyData.Maint
{
    public interface IMaintenanceCollectionWrapper
    {
        void DeleteSoftDeletedDocsOlderThan(Instant instant);
    }
}