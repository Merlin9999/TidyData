 #nullable disable
  namespace TidyData.Diag
{
    public class DiagnosticCollectionWrapper<TDocument> : IDiagnosticCollectionWrapper where TDocument : IDBDocument
    {
        private readonly IDictionary<Guid, TDocument> _collection;

        public DiagnosticCollectionWrapper(IDictionary<Guid, TDocument> collection)
        {
            this._collection = collection;
        }

        public void DeleteAllDocuments()
        {
            this._collection.Clear();
        }
    }
}