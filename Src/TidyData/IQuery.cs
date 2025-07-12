 #nullable disable
  namespace TidyData
{
    public interface IQuery<in TDataModel, TResult> : IQuery<TDataModel>
    {
        TResult Execute(TDataModel model, CollectionWrapperFactory factory);
    }

    public interface IAsyncQuery<in TDataModel, TResult> : IQuery<TDataModel>
    {
        Task<TResult> ExecuteAsync(TDataModel model, CollectionWrapperFactory factory);
    }

    public interface IQuery<in TDataModel>
    {
    }

    public class NullQuery<TDataModel, TResult> : IQuery<TDataModel, TResult>
        where TResult : class
    {
        public TResult Execute(TDataModel model, CollectionWrapperFactory factory)
        {
            return null;
        }
    }

    //public class EmptyQuery<TDataModel, TItem> : IQuery<TDataModel, IEnumerable<TItem>>
    //{
    //    public IEnumerable<TItem> Execute(TDataModel model, CollectionWrapperFactory factory)
    //    {
    //        return Enumerable.Empty<TItem>();
    //    }
    //}
}
