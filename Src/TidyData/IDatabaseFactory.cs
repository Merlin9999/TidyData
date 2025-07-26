namespace TidyData;

public interface IDatabaseFactory<TDataModel>
    where TDataModel : IDataModel, new()
{
    Task<IDatabase<TDataModel>> GetDatabaseAsync();
}