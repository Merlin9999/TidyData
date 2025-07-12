 #nullable disable
  namespace TidyData.Sync
{
    public record ListRegisteredDevicesRequest
    {
        public Guid AccountId { get; init; }
    }
}
