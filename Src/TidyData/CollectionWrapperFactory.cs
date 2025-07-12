 #nullable disable
 using System.Collections.Immutable;
 using System.Linq.Expressions;
 using NodaTime;

 namespace TidyData
{
    public class CollectionWrapperFactory
    {
        private readonly object _objLock = new object();
        private volatile IDataModel _dataModel;
        private volatile ImmutableDictionary<Type, ICollectionWrapper> _collectionDictionary;

        public CollectionWrapperFactory(IClock clock = null)
        {
            this.Clock = clock ?? SystemClock.Instance;
        }

        public IClock Clock { get; }

        public IEnumerable<ICollectionWrapper> GetCollections(IDataModel dataModel)
        {
            this.InitDataSource(dataModel, this.Clock);
            return this._collectionDictionary.Values;
        }

        public CollectionWrapper<TDocument> Get<TDataModel, TDocument>(TDataModel dataModel, Expression<Func<TDataModel, IDictionary<Guid, TDocument>>> lambda)
            where TDataModel : IDataModel
            where TDocument : IDBDocument
        {
            return this.Get<TDataModel, TDocument>(dataModel);
        }

        public CollectionWrapper<TDocument> Get<TDataModel, TDocument>(TDataModel dataModel) 
            where TDataModel : IDataModel 
            where TDocument : IDBDocument
        {
            this.InitDataSource(dataModel, this.Clock);

            if (this._collectionDictionary.TryGetValue(typeof(TDocument), out ICollectionWrapper collectionWrapper))
                return (CollectionWrapper<TDocument>)collectionWrapper;

            throw new ArgumentException($"Collection of {nameof(TDocument)} was not found!");
        }

        private void InitDataSource(IDataModel dataModel, IClock clock)
        {
            lock (this._objLock)
            {
                if (this._dataModel == dataModel)
                    return;

                this._dataModel = dataModel;
                this._collectionDictionary = dataModel.GetCollections(clock ?? SystemClock.Instance)
                    .ToImmutableDictionary(x => x.DocumentType);
            }
        }
    }
}