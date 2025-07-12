 #nullable disable
  namespace TidyData.Query
{
    public record GetAllQuery<TDataModel, TDocument> : IQuery<TDataModel, List<TDocument>>
        where TDataModel : IDataModel
        where TDocument : IDBDocument
    {
        public bool IncludeDeleted { get; init; }

        public virtual List<TDocument> Execute(TDataModel model, CollectionWrapperFactory factory)
        {
            return factory.Get<TDataModel, TDocument>(model)
                .GetAll(this.IncludeDeleted)
                .ToList();
        }
    }
}