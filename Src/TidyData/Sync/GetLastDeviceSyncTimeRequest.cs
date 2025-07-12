 #nullable disable
  namespace TidyData.Sync
{
    public class GetLastDeviceSyncTimeRequest
    {
        public Guid AccountId { get; set; }
        public Guid DeviceId { get; set; }
    }
}