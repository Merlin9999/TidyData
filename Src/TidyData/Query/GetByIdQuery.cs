 #nullable disable
  namespace TidyData.Query
{
    public record GetByIdQuery<TDataModel, TDocument> : IQuery<TDataModel, TDocument>
        where TDataModel : IDataModel
        where TDocument : IDBDocument
    {
        public Guid DocumentId { get; init; }

        public virtual TDocument Execute(TDataModel model, CollectionWrapperFactory factory)
        {
            return factory.Get<TDataModel, TDocument>(model)
                .TryGetById(this.DocumentId);
        }
    }
}