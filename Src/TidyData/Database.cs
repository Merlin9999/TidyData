 #nullable disable
 using NodaTime;
 using TidyData.Msg.Notifications;
 using TidyData.Storage;
 using TidyData.Sync;
 using TidyMediator;
 using TidyUtility.Core;

 namespace TidyData
{
    public record DBMetaData
    {
        public Guid AccountId { get; init; }
    }

    public class Database<TDataModel> : IDatabase<TDataModel>
        where TDataModel : class, IDataModel, new()
    {
        private readonly SerialQueue _commandAndQueryQueue;
        private readonly IDBStorage<TDataModel> _dbStorage;
        private readonly IClock _clock;
        private readonly DBMetaData _metaData;
        private readonly IMediator _mediator;
        private readonly CollectionWrapperFactory _collectionWrapperFactory;

        public Database(IDBStorage<TDataModel> dbStorage, IClock clock = null,
            DBMetaData metaData = null, IMediator mediator = null)
        {
            this._dbStorage = dbStorage;
            this._clock = clock ?? SystemClock.Instance;
            this._metaData = metaData;
            this._mediator = mediator;
            this._commandAndQueryQueue = new SerialQueue();
            this._collectionWrapperFactory = new CollectionWrapperFactory(this._clock);
        }

        public async Task ExecuteAsync(ICommand<TDataModel> command)
        {
            async Task AsyncCommand()
            {
                Guid commandId = Guid.NewGuid();
                await this.PublishStartEventAsync(command, commandId);

                try
                {
                    TDataModel dataModel = await this._dbStorage.ReadAndLockAsync();
                    command.Execute(dataModel, this._collectionWrapperFactory);
                    await this._dbStorage.UpdateAndReleaseLockAsync(dataModel);
                }
                catch (Exception exc)
                {
                    await this.PublishEndEventAsync(command, commandId, exc);
                    throw;
                }

                await this.PublishEndEventAsync(command, commandId);
            }

            if (command is AggregateCommand<TDataModel> aggregateCommand)
                if (aggregateCommand.HasEmptyCommandList())
                    return;

            await this._commandAndQueryQueue.Enqueue(AsyncCommand);
        }

        public async Task ExecuteAsync(IAsyncCommand<TDataModel> command)
        {
            async Task AsyncCommand()
            {
                Guid commandId = Guid.NewGuid();
                await this.PublishStartEventAsync(command, commandId);

                try
                {
                    TDataModel dataModel = await this._dbStorage.ReadAndLockAsync();
                    await command.ExecuteAsync(dataModel, this._collectionWrapperFactory);
                    await this._dbStorage.UpdateAndReleaseLockAsync(dataModel);
                }
                catch (Exception exc)
                {
                    await this.PublishEndEventAsync(command, commandId, exc);
                    throw;
                }

                await this.PublishEndEventAsync(command, commandId);
            }

            await this._commandAndQueryQueue.Enqueue(AsyncCommand);
        }

        public async Task<TResult> ExecuteAsync<TResult>(IQuery<TDataModel, TResult> query)
        {
            async Task<TResult> AsyncQuery()
            {
                TDataModel dataModel = await this._dbStorage.ReadOnlyReadAsync();
                return query.Execute(dataModel, this._collectionWrapperFactory);
            }

            return await this._commandAndQueryQueue.Enqueue(AsyncQuery);
        }

        public async Task<TResult> ExecuteAsync<TResult>(IAsyncQuery<TDataModel, TResult> query)
        {
            async Task<TResult> AsyncQuery()
            {
                TDataModel dataModel = await this._dbStorage.ReadOnlyReadAsync();
                return await query.ExecuteAsync(dataModel, this._collectionWrapperFactory);
            }

            return await this._commandAndQueryQueue.Enqueue(AsyncQuery);
        }

        public async Task<TResult> ExecuteAsync<TResult>(ICommandAndQuery<TDataModel, TResult> commandAndQuery)
        {
            async Task<TResult> AsyncCommandAndQuery()
            {
                Guid commandId = Guid.NewGuid();
                await this.PublishStartEventAsync(commandAndQuery, commandId);

                TResult result;
                try
                {
                    TDataModel dataModel = await this._dbStorage.ReadAndLockAsync();
                    result = commandAndQuery.Execute(dataModel, this._collectionWrapperFactory);
                    await this._dbStorage.UpdateAndReleaseLockAsync(dataModel);
                }
                catch (Exception exc)
                {
                    await this.PublishEndEventAsync(commandAndQuery, commandId, exc);
                    throw;
                }

                await this.PublishEndEventAsync(commandAndQuery, commandId);

                return result;
            }

            return await this._commandAndQueryQueue.Enqueue(AsyncCommandAndQuery);
        }

        public async Task<TResult> ExecuteAsync<TResult>(IAsyncCommandAndQuery<TDataModel, TResult> commandAndQuery)
        {
            async Task<TResult> AsyncCommandAndQuery()
            {
                Guid commandId = Guid.NewGuid();
                await this.PublishStartEventAsync(commandAndQuery, commandId);

                TResult result;
                try
                {
                    TDataModel dataModel = await this._dbStorage.ReadAndLockAsync();
                    result = await commandAndQuery.ExecuteAsync(dataModel, this._collectionWrapperFactory);
                    await this._dbStorage.UpdateAndReleaseLockAsync(dataModel);
                }
                catch (Exception exc)
                {
                    await this.PublishEndEventAsync(commandAndQuery, commandId, exc);
                    throw;
                }

                await this.PublishEndEventAsync(commandAndQuery, commandId);

                return result;
            }

            return await this._commandAndQueryQueue.Enqueue(AsyncCommandAndQuery);
        }

        internal async Task DeleteDatabaseAsync()
        {
            IDBStorage<TDataModel> dbStorage = this._dbStorage;
            if (dbStorage is CacheDBStorageAdapter<TDataModel>)
                dbStorage = ((CacheDBStorageAdapter<TDataModel>) this._dbStorage).StorageImpl;
            await ((DBStorageBase<TDataModel>) dbStorage).DeleteDatabaseAsync();
        }

        private async Task PublishStartEventAsync(object command, Guid commandId)
        {
            if (this._mediator == null || this._metaData == null)
                return;

            await this._mediator.PublishAsync(new DBCommandStart.Event()
            {
                AccountId = this._metaData.AccountId,
                CommandId = commandId,
                IsClientSyncCommand = command is IClientSyncCommand,
                CommandType = command.GetType(),
            }, CancellationToken.None);
        }

        private async Task PublishEndEventAsync(object command, Guid commandId, Exception exception = null)
        {
            if (this._mediator == null || this._metaData == null)
                return;

            await this._mediator.PublishAsync(new DBCommandEnd.Event()
            {
                AccountId = this._metaData.AccountId,
                CommandId = commandId,
                IsClientSyncCommand = command is IClientSyncCommand,
                CommandType = command.GetType(),
                Exception = exception,
            }, CancellationToken.None);
        }
    }
}