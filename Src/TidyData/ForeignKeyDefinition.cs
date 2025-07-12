 #nullable disable
 using System.Collections.Immutable;

 namespace TidyData
{
    public interface IForeignKeyDefinition
    {
        string PrimaryKeyCollectionName { get; }
        ImmutableHashSet<Guid> GetReferencedKeys(bool includeSoftDeletedForeignKeyDocuments);
    }

    public class ForeignKeyDefinition<TForeignDBDocument> : IForeignKeyDefinition, IForeignKeyIndex<TForeignDBDocument>
        where TForeignDBDocument : IDBDocument
    {
        public ForeignKeyDefinition(string primaryKeyCollectionName,
            IDictionary<Guid, TForeignDBDocument> foreignKeyCollection, Func<TForeignDBDocument, Guid> foreignKeyFunc)
        : this(primaryKeyCollectionName, foreignKeyCollection, x => new[] { foreignKeyFunc(x) })
        {
        }

        public ForeignKeyDefinition(string primaryKeyCollectionName,
            IDictionary<Guid, TForeignDBDocument> foreignKeyCollection, Func<TForeignDBDocument, Guid?> foreignKeyFunc)
            : this(primaryKeyCollectionName, foreignKeyCollection, 
                x => new[] { foreignKeyFunc(x) }.Where(x => x != null).Cast<Guid>())
        {
        }

        public ForeignKeyDefinition(string primaryKeyCollectionName, 
            IDictionary<Guid, TForeignDBDocument> foreignKeyCollection, 
            Func<TForeignDBDocument, IEnumerable<Guid>> foreignKeyFunc)
        {
            this.PrimaryKeyCollectionName = primaryKeyCollectionName;
            this.ForeignKeyCollection = foreignKeyCollection;
            this.ForeignKeyFunc = foreignDBDocument => foreignKeyFunc(foreignDBDocument) ?? Enumerable.Empty<Guid>();
        }

        public string PrimaryKeyCollectionName { get; }
        public IDictionary<Guid, TForeignDBDocument> ForeignKeyCollection { get; }
        public Func<TForeignDBDocument, IEnumerable<Guid>> ForeignKeyFunc { get; }

        public ImmutableHashSet<Guid> GetReferencedKeys(bool includeSoftDeletedForeignKeyDocuments)
        {
            if (includeSoftDeletedForeignKeyDocuments)
                return this.BuildIndex().Select(x => x.Key).ToImmutableHashSet();

            return this.BuildIndex()
                .Where(x => x.Any(y => !((IDBDocument) y.ForeignDBDocument).Meta.Deleted))
                .Select(x => x.Key).ToImmutableHashSet();
        }
    }

    public interface IForeignKeyIndex<TForeignDBDocument> where TForeignDBDocument : IDBDocument
    {
        IDictionary<Guid, TForeignDBDocument> ForeignKeyCollection { get; }
        Func<TForeignDBDocument, IEnumerable<Guid>> ForeignKeyFunc { get; }
    }

    public static class ForeignKeyIndexExtensions
    {
        public static ILookup<Guid, ForeignKeyInstance<TForeignDBDocument>> BuildIndex<TForeignDBDocument>(this IForeignKeyIndex<TForeignDBDocument> instance)
            where TForeignDBDocument : IDBDocument
        {
            return instance.ForeignKeyCollection
                .SelectMany(x => instance.ForeignKeyFunc(x.Value),
                    (x, fk) => new {ForeignDoc = x.Value, ForeignKey = fk})
                .ToLookup(
                    x => x.ForeignKey,
                    x => new ForeignKeyInstance<TForeignDBDocument>()
                    {
                        ForeignKey = x.ForeignKey,
                        ForeignDBDocument = x.ForeignDoc,
                    });
        }
    }

    public record ForeignKeyInstance<TForeignDBDocument>
        where TForeignDBDocument : IDBDocument
    {
        public Guid ForeignKey { get; init; }
        public TForeignDBDocument ForeignDBDocument { get; init; }
    }

}