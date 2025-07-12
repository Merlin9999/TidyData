 #nullable disable
  namespace TidyData
{
    public interface ICommand<in TDataModel>
    {
        void Execute(TDataModel model, CollectionWrapperFactory factory);
    }

    public interface IAsyncCommand<in TDataModel>
    {
        Task ExecuteAsync(TDataModel model, CollectionWrapperFactory factory);
    }
}