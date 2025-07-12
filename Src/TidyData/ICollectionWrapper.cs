 #nullable disable
 using TidyData.Diag;
 using TidyData.Maint;
 using TidyData.Sync;

 namespace TidyData
{
    public interface ICollectionWrapper
    {
        string CollectionName { get; }
        Type DocumentType { get; }
        ISyncCollectionWrapper ForDBSync { get; }
        IMaintenanceCollectionWrapper ForMaintenance { get; }
        IDiagnosticCollectionWrapper ForDiagnostics { get; }
    }
}