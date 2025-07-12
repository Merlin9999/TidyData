 #nullable disable
namespace TidyData.Command
{
    public record UpsertCommand<TDataModel, TDocument> : ICommand<TDataModel>
        where TDataModel : IDataModel
        where TDocument : IDBDocument
    {
        public TDocument Document { get; init; }

        public virtual void Execute(TDataModel model, CollectionWrapperFactory factory)
        {
            factory.Get<TDataModel, TDocument>(model)
                .Upsert(this.Document);
        }
    }
}