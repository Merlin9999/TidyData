 #nullable disable
 using System.Collections.Immutable;

 namespace TidyData.Command
{
    public record DeleteCommand<TDataModel, TDocument> : ICommand<TDataModel>
        where TDataModel : IDataModel
        where TDocument : IDBDocument
    {
        private readonly ImmutableList<Guid> _documentIdsToDelete;

        public Guid DocumentIdToDelete { init => this._documentIdsToDelete = ImmutableList<Guid>.Empty.Add(value); }
        public IEnumerable<Guid> DocumentIdsToDelete
        {
            get => this._documentIdsToDelete;
            init => this._documentIdsToDelete = ImmutableList<Guid>.Empty.AddRange(value);
        }

        public virtual void Execute(TDataModel model, CollectionWrapperFactory factory)
        {
            CollectionWrapper<TDocument> colWrapper = factory.Get<TDataModel, TDocument>(model);
            foreach (Guid guid in this.DocumentIdsToDelete)
                colWrapper.TrySoftDelete(guid);
        }
    }
}