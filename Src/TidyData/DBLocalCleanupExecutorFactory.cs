using NodaTime;

namespace TidyData;

public interface IDBLocalCleanupExecutorFactory<TDataModel>
    where TDataModel : IDataModel, new()
{
    Task<IDBLocalCleanupExecutor> GetExecutor();
}

public class DBLocalCleanupExecutorFactory<TDataModel> : IDBLocalCleanupExecutorFactory<TDataModel>
    where TDataModel : IDataModel, new()
{
    private readonly IDatabaseFactory<TDataModel> _dbFactory;
    private readonly DBLocalAlgorithmSettings _dbLocalAlgSettings;
    private readonly IClock _clock;
    public DBLocalCleanupExecutorFactory(IDatabaseFactory<TDataModel> dbFactory, DBLocalAlgorithmSettings dbLocalAlgSettings, IClock clock)
    {
        _dbFactory = dbFactory;
        _dbLocalAlgSettings = dbLocalAlgSettings;
        _clock = clock;
    }

    public async Task<IDBLocalCleanupExecutor> GetExecutor()
    {
        IDatabase<TDataModel> db = await _dbFactory.GetDatabaseAsync();
        return new DBLocalCleanupExecutor<TDataModel>(db, _dbLocalAlgSettings, _clock);
    }
}
