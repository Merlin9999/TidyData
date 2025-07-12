 #nullable disable
 using System.Collections.Immutable;

 namespace TidyData.Query
{
    public record GetCollectionListQuery<TDataModel> : IQuery<TDataModel, ImmutableList<ICollectionWrapper>>
        where TDataModel : IDataModel
    {
        public virtual ImmutableList<ICollectionWrapper> Execute(TDataModel model, CollectionWrapperFactory factory)
        {
            return factory.GetCollections(model).ToImmutableList();
        }
    }
}