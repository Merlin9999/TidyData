 #nullable disable
 using System.Collections.Immutable;
 using System.Reflection;
 using Dynamitey;
 using NodaTime;
 using TidyData.Diag;
 using TidyData.Maint;
 using TidyData.Sync;
 using TidyUtility.Core;
 using TidyUtility.Core.Extensions;

namespace TidyData
{
    public class CollectionWrapper
    {
        protected static ImmutableDictionary<Type, Func<IDBDocument, DocumentVersion, IDBDocument>>
            VersionUpdateFactoryLookup =
                ImmutableDictionary<Type, Func<IDBDocument, DocumentVersion, IDBDocument>>.Empty;

        protected static ImmutableDictionary<Type, Func<IDBDocument, DocumentVersion, DocumentMetaData, IDBDocument>>
            VersionMetaUpdateFactoryLookup =
                ImmutableDictionary<Type, Func<IDBDocument, DocumentVersion, DocumentMetaData, IDBDocument>>.Empty;
    }

    public class CollectionWrapper<TDocument> : CollectionWrapper, ICollectionWrapper
        where TDocument : IDBDocument
    {
        private readonly IDictionary<Guid, TDocument> _collection;
        private readonly List<IForeignKeyDefinition> _foreignKeyDefinitions;
        private readonly IClock _clock;

        public CollectionWrapper(IDataModel dataModel, string collectionName, IDictionary<Guid, TDocument> collection, IClock clock = null)
        {
            this._collection = collection;
            this._foreignKeyDefinitions = dataModel.GetForeignKeyDefinitions()
                .Where(x => x.PrimaryKeyCollectionName == collectionName).ToList();
            this._clock = clock ?? SystemClock.Instance;
            this.CollectionName = collectionName;
            this.DocumentType = typeof(TDocument);
            this.ForDBSync = new SyncCollectionWrapper<TDocument>(collectionName, collection);
            Func<ImmutableHashSet<Guid>> getIdsReferencedAsForeignKeysFunc = () => this._foreignKeyDefinitions.SelectMany(x => x.GetReferencedKeys(true)).ToImmutableHashSet();
            this.ForMaintenance = new MaintenanceCollectionWrapper<TDocument>(collection, getIdsReferencedAsForeignKeysFunc);
            this.ForDiagnostics = new DiagnosticCollectionWrapper<TDocument>(collection);
        }
        public string CollectionName { get; }
        public Type DocumentType { get; }

        public ISyncCollectionWrapper ForDBSync { get; }
        public IMaintenanceCollectionWrapper ForMaintenance { get; }
        public IDiagnosticCollectionWrapper ForDiagnostics { get; }
        
        public void Insert(TDocument documentToInsert)
        {
            if (!this.TryInsert(documentToInsert))
                throw new DatabaseException($"Document insert failed! Document of type {typeof(TDocument).Name} already exists with id: {documentToInsert.Id}");
        }

        public bool TryInsert(TDocument documentToInsert)
        {
            if (this._collection.ContainsKey(documentToInsert.Id))
                return false;

            Func<IDBDocument, DocumentVersion, DocumentMetaData, IDBDocument> versionMetaUpdateFactory = GetFuncToUpdateVersionAndMeta();

            var metaData = documentToInsert.Meta ?? new DocumentMetaData();
            documentToInsert = (TDocument)versionMetaUpdateFactory(documentToInsert, DocumentVersionExtensions.NewVersion(null, clock: this._clock), metaData);
            this._collection.Add(documentToInsert.Id, documentToInsert);
            return true;
        }

        public void Update(TDocument documentToUpdate)
        {
            if (!this.TryUpdate(documentToUpdate))
                throw new DatabaseException($"Document update failed! Document of type {typeof(TDocument).Name} not found with id: {documentToUpdate.Id}");
        }

        public bool TryUpdate(TDocument documentToUpdate)
        {
            if (!this._collection.ContainsKey(documentToUpdate.Id))
                return false;

            this.Upsert(documentToUpdate);
            return true;
        }

        public void Upsert(TDocument documentToUpsert)
        {
            Func<IDBDocument, DocumentVersion, IDBDocument> versionUpdateFactory = GetFuncToUpdateVersion();
            documentToUpsert = (TDocument)versionUpdateFactory(documentToUpsert, DocumentVersionExtensions.NewVersion(null, clock: this._clock));
            this._collection[documentToUpsert.Id] = documentToUpsert;
        }

        public bool SafeToSoftDelete(Guid documentIdToDelete)
        { 
            ImmutableHashSet<Guid> foreignKeys = this.GetAllForeignKeys();
            return !foreignKeys.Contains(documentIdToDelete);
        }

        public ImmutableHashSet<Guid> GetAllForeignKeys()
        {
            return this._foreignKeyDefinitions
                .Where(fdk => fdk.PrimaryKeyCollectionName == this.CollectionName)
                .SelectMany(fk => fk.GetReferencedKeys(false))
                .ToImmutableHashSet();
        }

        public void SoftDelete(Guid documentIdToDelete)
        {
            if (!this.SafeToSoftDelete(documentIdToDelete))
                throw new DatabaseException($"Document soft-delete failed! Found a document with a foreign key to this document of type {typeof(TDocument).Name} with id: {documentIdToDelete}");

            if (!this.TrySoftDeleteImpl(documentIdToDelete))
                throw new DatabaseException($"Document soft-delete failed! Document of type {typeof(TDocument).Name} not found with id: {documentIdToDelete}");
        }

        public bool TrySoftDelete(Guid documentIdToDelete)
        {
            if (!this.SafeToSoftDelete(documentIdToDelete))
                return false;

            return this.TrySoftDeleteImpl(documentIdToDelete);
        }

        private bool TrySoftDeleteImpl(Guid documentIdToDelete)
        {
            TDocument documentToSoftDelete = this._collection.TryGetValue(documentIdToDelete);
            if (documentToSoftDelete == null)
                return false;

            Func<IDBDocument, DocumentVersion, DocumentMetaData, IDBDocument> versionMetaUpdateFactory =
                GetFuncToUpdateVersionAndMeta();
            var softDeletedMetaData = documentToSoftDelete.Meta with { Deleted = true };
            documentToSoftDelete = (TDocument) versionMetaUpdateFactory(documentToSoftDelete,
                DocumentVersionExtensions.NewVersion(null, clock: this._clock), softDeletedMetaData);
            this._collection[documentToSoftDelete.Id] = documentToSoftDelete;
            return true;
        }

        public IEnumerable<TDocument> GetAll(bool includeDeleted = false)
        {
            IEnumerable<TDocument> result = this._collection.Values;

            if (!includeDeleted)
                result = result.Where(d => !d.Meta.Deleted);

            return result;
        }

        public TDocument GetById(Guid id)
        {
            if (this._collection.TryGetValue(id, out TDocument document))
                return document;
            throw new DatabaseException($"Get Document by Id failed! Document of type {typeof(TDocument).Name} not found with id: {id}");
        }

        public TDocument TryGetById(Guid id)
        {
            return this._collection.TryGetValue(id);
        }

        private static Func<IDBDocument, DocumentVersion, IDBDocument> GetFuncToUpdateVersion()
        {
            var localLookup = VersionUpdateFactoryLookup;
            Func<IDBDocument, DocumentVersion, IDBDocument> factoryMethod = localLookup.TryGetValue(typeof(TDocument));
            if (factoryMethod != null)
                return factoryMethod;

            factoryMethod = BuildFactoryMethodForUpdatedVersionFromDefaultConstructor() ??
                BuildFactoryMethodForUpdatedVersionFromStdConstructor();

            localLookup = localLookup.Add(typeof(TDocument), factoryMethod);

            Interlocked.CompareExchange(ref VersionUpdateFactoryLookup, localLookup, VersionUpdateFactoryLookup);

            return factoryMethod;
        }

        private static Func<IDBDocument, DocumentVersion, IDBDocument> BuildFactoryMethodForUpdatedVersionFromDefaultConstructor()
        {
            // Used for records. With c# 9, changed IDBDocument interface to use init for Version and Meta properties.
            // Records should have a protected copy constructor. With it, we will create a clone and then use reflection
            // to initialize the updated version property.
            FactoryMethod<IDBDocument> cloneMethod = typeof(TDocument)
                .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(ct =>
                {
                    ParameterInfo[] parameters = ct.GetParameters();
                    if (parameters.Length == 1 && parameters[0].ParameterType == typeof(TDocument))
                        return true;

                    return false;
                })
                .Select(ct => Factory.MethodBuilder<IDBDocument>(ct))
                .SingleOrDefault();

            if (cloneMethod == null)
                return null;

            PropertyInfo versionPropInfo = typeof(TDocument).GetProperty("Version");

            Func<IDBDocument, DocumentVersion, IDBDocument> factoryMethod = (cloneFrom, version) =>
            {
                IDBDocument clone = cloneMethod(cloneFrom);
                versionPropInfo.SetValue(clone, version);

                return clone; // Clone updated with new version.
            };

            return factoryMethod;
        }

        private static Func<IDBDocument, DocumentVersion, IDBDocument> BuildFactoryMethodForUpdatedVersionFromStdConstructor()
        {
            var ctorData = typeof(TDocument).GetConstructors()
                .Where(ct =>
                {
                    ParameterInfo[] parameters = ct.GetParameters();
                    if (!parameters.Any())
                        return false;
                    if (parameters.Any(p => !p.HasDefaultValue))
                        return false;

                    return true;
                })
                .Select(ct =>
                {
                    ParameterInfo[] parameters = ct.GetParameters();

                    ParameterInfo cloneFromParam =
                        parameters.FirstOrDefault(
                            p => p.Name == "cloneFrom" && p.ParameterType == typeof(TDocument)) ??
                        parameters.SingleOrDefault(p => p.ParameterType == typeof(TDocument));
                    ParameterInfo versionParam =
                        parameters.FirstOrDefault(p =>
                            p.Name == "version" && p.ParameterType == typeof(DocumentVersion)) ??
                        parameters.SingleOrDefault(p => p.ParameterType == typeof(DocumentVersion));

                    return new {Constructor = ct, CloneFromParam = cloneFromParam, VersionParam = versionParam};
                })
                .Single(x => x.CloneFromParam != null && x.VersionParam != null);

            var arg = InvokeArg.Create;
            Func<IDBDocument, DocumentVersion, IDBDocument> factoryMethod =
                (cloneFrom, version) => (TDocument) Dynamic.InvokeConstructor(typeof(TDocument),
                    arg(ctorData.CloneFromParam.Name, cloneFrom), arg(ctorData.VersionParam.Name, version));
            return factoryMethod;
        }

        private static Func<IDBDocument, DocumentVersion, DocumentMetaData, IDBDocument> GetFuncToUpdateVersionAndMeta()
        {
            var localLookup = VersionMetaUpdateFactoryLookup;
            var factoryMethod = localLookup.TryGetValue(typeof(TDocument));
            if (factoryMethod != null)
                return factoryMethod;

            factoryMethod = BuildFactoryMethodForUpdatedVersionAndMetaFromDefaultConstructor() ??
                BuildFactoryMethodForUpdatedVersionAndMetaFromStdConstructor();

            localLookup = localLookup.Add(typeof(TDocument), factoryMethod);

            Interlocked.CompareExchange(ref VersionMetaUpdateFactoryLookup, localLookup, VersionMetaUpdateFactoryLookup);

            return factoryMethod;
        }

        private static Func<IDBDocument, DocumentVersion, DocumentMetaData, IDBDocument> BuildFactoryMethodForUpdatedVersionAndMetaFromDefaultConstructor()
        {
            // Used for records. With c# 9, changed IDBDocument interface to use init for Version and Meta properties.
            // Records should have a protected copy constructor. With it, we will create a clone and then use reflection
            // to initialize the updated version property.
            FactoryMethod<IDBDocument> cloneMethod = typeof(TDocument)
                .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(ct =>
                {
                    ParameterInfo[] parameters = ct.GetParameters();
                    if (parameters.Length == 1 && parameters[0].ParameterType == typeof(TDocument))
                        return true;

                    return false;
                })
                .Select(ct => Factory.MethodBuilder<IDBDocument>(ct))
                .SingleOrDefault();

            if (cloneMethod == null)
                return null;

            PropertyInfo versionPropInfo = typeof(TDocument).GetProperty("Version");
            PropertyInfo metaPropInfo = typeof(TDocument).GetProperty("Meta");

            Func<IDBDocument, DocumentVersion, DocumentMetaData, IDBDocument> factoryMethod = (cloneFrom, version, meta) =>
            {
                IDBDocument clone = cloneMethod(cloneFrom);
                versionPropInfo.SetValue(clone, version);
                metaPropInfo.SetValue(clone, meta);

                return clone; // Clone updated with new version and meta.
            };

            return factoryMethod;
        }

        private static Func<IDBDocument, DocumentVersion, DocumentMetaData, IDBDocument> BuildFactoryMethodForUpdatedVersionAndMetaFromStdConstructor()
        {
            Func<IDBDocument, DocumentVersion, DocumentMetaData, IDBDocument> factoryMethod;
            var ctorData = typeof(TDocument).GetConstructors()
                .Where(ct =>
                {
                    ParameterInfo[] parameters = ct.GetParameters();
                    if (!parameters.Any())
                        return false;
                    if (parameters.Any(p => !p.HasDefaultValue))
                        return false;

                    return true;
                })
                .Select(ct =>
                {
                    ParameterInfo[] parameters = ct.GetParameters();

                    ParameterInfo cloneFromParam =
                        parameters.FirstOrDefault(
                            p => p.Name == "cloneFrom" && p.ParameterType == typeof(TDocument)) ??
                        parameters.SingleOrDefault(p => p.ParameterType == typeof(TDocument));
                    ParameterInfo versionParam =
                        parameters.FirstOrDefault(p =>
                            p.Name == "version" && p.ParameterType == typeof(DocumentVersion)) ??
                        parameters.SingleOrDefault(p => p.ParameterType == typeof(DocumentVersion));
                    ParameterInfo metaParam =
                        parameters.FirstOrDefault(p =>
                            p.Name == "meta" && p.ParameterType == typeof(DocumentMetaData)) ??
                        parameters.SingleOrDefault(p => p.ParameterType == typeof(DocumentMetaData));

                    return new
                    {
                        Constructor = ct, CloneFromParam = cloneFromParam, VersionParam = versionParam, MetaParam = metaParam
                    };
                })
                .First(x => x.CloneFromParam != null && x.VersionParam != null && x.MetaParam != null);

            var arg = InvokeArg.Create;
            factoryMethod = (cloneFrom, version, meta) => (TDocument) Dynamic.InvokeConstructor(typeof(TDocument),
                arg(ctorData.CloneFromParam.Name, cloneFrom), arg(ctorData.VersionParam.Name, version),
                arg(ctorData.MetaParam.Name, meta));
            return factoryMethod;
        }
    }
}
