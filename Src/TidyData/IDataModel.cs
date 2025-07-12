 #nullable disable
 using System.Collections.Immutable;
 using NodaTime;

 namespace TidyData
{
    public interface IDataModel
    {
        IEnumerable<ICollectionWrapper> GetCollections(IClock clock = null);
        IEnumerable<IForeignKeyDefinition> GetForeignKeyDefinitions();
    }

    public interface IClientDataModel : IDataModel
    {
        Instant? LastSync { get; set; }
    }

    public interface IServerDataModel : IDataModel
    {
        ImmutableDictionary<Guid, DeviceInformation> RemoteDeviceLookup { get; set; }
    }

    public record DeviceInformation
    {
        public Guid Id { get; init; }
        public string Name { get; init; }
        public Instant LastSyncTimeStamp { get; init; }
    }
}
