 #nullable disable
 using System.Collections.Immutable;

 namespace TidyData.Query
{
    public record GetAllOrphanedQuery<TDataModel, TDocument> : IQuery<TDataModel, ImmutableList<TDocument>>
        where TDataModel : IDataModel
        where TDocument : IDBDocument
    {
        public bool IncludeDeleted { get; init; }

        public virtual ImmutableList<TDocument> Execute(TDataModel model, CollectionWrapperFactory factory)
        {
            CollectionWrapper<TDocument> collectionWrapper = factory.Get<TDataModel, TDocument>(model);
            ImmutableHashSet<Guid> allForeignKeys = collectionWrapper.GetAllForeignKeys();

            return collectionWrapper
                .GetAll(this.IncludeDeleted)
                .Where(x => !allForeignKeys.Contains(x.Id))
                .ToImmutableList();
        }
    }
}