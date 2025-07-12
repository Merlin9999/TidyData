#nullable disable
using System.Collections.Immutable;
using NodaTime;
using TidyUtility.Data.Json;

// ReSharper disable CheckNamespace
namespace TidyData.Tests._Shared_Synced.TestModel;

[SafeToSerialize(IncludeNestedDerived = true)]
public class TestDataModel : IDataModel
{
    public Dictionary<Guid, TestDocument> Docs { get; } = new Dictionary<Guid, TestDocument>();

    public Dictionary<Guid, TestDocumentWithForeignKey> DocsWithForeignKey { get; } =
        new Dictionary<Guid, TestDocumentWithForeignKey>();

    public IEnumerable<ICollectionWrapper> GetCollections(IClock clock = null)
    {
        yield return new CollectionWrapper<TestDocument>(this, nameof(this.Docs), this.Docs, clock);
        yield return new CollectionWrapper<TestDocumentWithForeignKey>(this, nameof(this.DocsWithForeignKey),
            this.DocsWithForeignKey, clock);
    }

    public IEnumerable<IForeignKeyDefinition> GetForeignKeyDefinitions()
    {
        yield return new ForeignKeyDefinition<TestDocumentWithForeignKey>(nameof(this.Docs),
            this.DocsWithForeignKey, x => x.TestDocumentId);
    }
}

public class ClientTestDataModel : TestDataModel, IClientDataModel
{
    public Instant? LastSync { get; set; }
}

public class ServerTestDataModel : TestDataModel, IServerDataModel
{
    private volatile ImmutableDictionary<Guid, DeviceInformation> _remoteDeviceLookup;

    public ImmutableDictionary<Guid, DeviceInformation> RemoteDeviceLookup
    {
        get => this._remoteDeviceLookup ??= ImmutableDictionary<Guid, DeviceInformation>.Empty;
        set => this._remoteDeviceLookup = value;
    }
}