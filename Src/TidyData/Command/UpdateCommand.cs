 #nullable disable
namespace TidyData.Command
{
    public record UpdateCommand<TDataModel, TDocument> : ICommand<TDataModel>
        where TDataModel : IDataModel
        where TDocument : IDBDocument
    {
        public TDocument Updated { get; init; }

        public virtual void Execute(TDataModel model, CollectionWrapperFactory factory)
        {
            factory.Get<TDataModel, TDocument>(model)
                .Update(this.Updated);
        }
    }
}