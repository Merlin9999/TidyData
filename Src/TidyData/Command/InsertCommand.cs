 #nullable disable
namespace TidyData.Command
{
    public record InsertCommand<TDataModel, TDocument> : ICommand<TDataModel>
        where TDataModel : IDataModel
        where TDocument : IDBDocument
    {
        public TDocument ToInsert { get; init; }

        public virtual void Execute(TDataModel model, CollectionWrapperFactory factory)
        {
            factory.Get<TDataModel, TDocument>(model)
                .Insert(this.ToInsert);
        }
    }
}