 #nullable disable
 using System.Collections.Immutable;

 namespace TidyData.Sync
{
    public record ListRegisteredDevicesResponse
    {
        public Guid AccountId { get; init; }
        public ImmutableList<DeviceInformation> Devices { get; init; }
    }
}
