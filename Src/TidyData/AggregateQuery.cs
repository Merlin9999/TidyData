 #nullable disable
  namespace TidyData
{
    public static class AggregateQuery
    {
        public static AggregateQuery<TModel, TResult1, TResult2> Create<TModel, TResult1, TResult2>(
            IQuery<TModel, TResult1> query1, IQuery<TModel, TResult2> query2)
        {
            return new AggregateQuery<TModel, TResult1, TResult2>()
            {
                Query1 = query1,
                Query2 = query2,
            };
        }

        public static AggregateQuery<TModel, TResult1, TResult2, TResult3> Create<TModel, TResult1, TResult2, TResult3>(
            IQuery<TModel, TResult1> query1, IQuery<TModel, TResult2> query2, IQuery<TModel, TResult3> query3)
        {
            return new AggregateQuery<TModel, TResult1, TResult2, TResult3>()
            {
                Query1 = query1,
                Query2 = query2,
                Query3 = query3,
            };
        }
    }

    public record AggregateQuery<TModel, TResult1, TResult2> : IQuery<TModel, (TResult1, TResult2)>
    {
        public IQuery<TModel, TResult1> Query1 { get; init; }
        public IQuery<TModel, TResult2> Query2 { get; init; }

        public (TResult1, TResult2) Execute(TModel model, CollectionWrapperFactory factory)
        {
            return (this.Query1.Execute(model, factory), this.Query2.Execute(model, factory));
        }
    }

    public record AggregateQuery<TModel, TResult1, TResult2, TResult3> : IQuery<TModel, (TResult1, TResult2, TResult3)>
    {
        public IQuery<TModel, TResult1> Query1 { get; init; }
        public IQuery<TModel, TResult2> Query2 { get; init; }
        public IQuery<TModel, TResult3> Query3 { get; init; }

        public (TResult1, TResult2, TResult3) Execute(TModel model, CollectionWrapperFactory factory)
        {
            return (this.Query1.Execute(model, factory), this.Query2.Execute(model, factory), this.Query3.Execute(model, factory));
        }
    }
}