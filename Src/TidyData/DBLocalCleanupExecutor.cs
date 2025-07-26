using NodaTime;
using TidyData.Maint.Command;

namespace TidyData;

public interface IDBLocalCleanupExecutor
{
    Task ExecuteAsync();
}

public class DBLocalCleanupExecutor<TDataModel> : IDBLocalCleanupExecutor
    where TDataModel : IDataModel, new()
{
    private readonly IDatabase<TDataModel> _db;
    private readonly DBLocalAlgorithmSettings _dbLocalAlgSettings;
    private readonly IClock _clock;

    public DBLocalCleanupExecutor(IDatabase<TDataModel> db, DBLocalAlgorithmSettings dbLocalAlgSettings, IClock clock)
    {
        _db = db;
        _dbLocalAlgSettings = dbLocalAlgSettings;
        _clock = clock;
    }

    public async Task ExecuteAsync()
    {
        var localDBCleanupCommand = new LocalDBCleanupCommand<TDataModel>(_dbLocalAlgSettings, _clock);

        await this._db.ExecuteAsync(localDBCleanupCommand);
    }
}