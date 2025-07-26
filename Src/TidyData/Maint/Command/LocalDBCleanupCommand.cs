#nullable disable
using NodaTime;

namespace TidyData.Maint.Command;

public class LocalDBCleanupCommand<TDataModel>(DBLocalAlgorithmSettings dbLocalAlgSettings, IClock clock)
    : ICommand<TDataModel>
    where TDataModel : IDataModel, new()
{
    public void Execute(TDataModel model, CollectionWrapperFactory factory)
    {
        List<ICollectionWrapper> localCollections = factory.GetCollections(model).ToList();
        CleanupEligibleSoftDeletedDocuments(localCollections, dbLocalAlgSettings, clock);
    }

    private static void CleanupEligibleSoftDeletedDocuments(List<ICollectionWrapper> localCollections, 
        DBLocalAlgorithmSettings dbLocalAlgSettings, IClock clock)
    {
        Instant cleanupStart = clock.GetCurrentInstant();
        Instant deleteTimeStamp = cleanupStart.Minus(dbLocalAlgSettings.MinAgeToDeleteSoftDeletedDocs);

        foreach (ICollectionWrapper collection in localCollections)
            collection.ForMaintenance.DeleteSoftDeletedDocsOlderThan(deleteTimeStamp);
    }
}