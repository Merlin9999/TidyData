 #nullable disable
  namespace TidyData
{
    public interface ICommandAndQuery<in TDataModel, TResult> : IQuery<TDataModel>
    {
        TResult Execute(TDataModel model, CollectionWrapperFactory factory);
    }

    public interface IAsyncCommandAndQuery<in TDataModel, TResult> : IQuery<TDataModel>
    {
        Task<TResult> ExecuteAsync(TDataModel model, CollectionWrapperFactory factory);
    }
}
