 #nullable disable
  namespace TidyData.Sync;

public record DeleteRegisteredDeviceRequest
{
    public Guid AccountId { get; init; }
    public Guid DeviceId { get; init; }
}