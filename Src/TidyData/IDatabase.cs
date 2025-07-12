 #nullable disable
  namespace TidyData
{
    public interface IDatabase<TDataModel> 
        where TDataModel : IDataModel, new()
    {
        Task ExecuteAsync(ICommand<TDataModel> command);
        Task ExecuteAsync(IAsyncCommand<TDataModel> command);
        Task<TResult> ExecuteAsync<TResult>(IQuery<TDataModel, TResult> query);
        Task<TResult> ExecuteAsync<TResult>(IAsyncQuery<TDataModel, TResult> query);
        Task<TResult> ExecuteAsync<TResult>(ICommandAndQuery<TDataModel, TResult> commandAndQuery);
        Task<TResult> ExecuteAsync<TResult>(IAsyncCommandAndQuery<TDataModel, TResult> commandAndQuery);
    }
}