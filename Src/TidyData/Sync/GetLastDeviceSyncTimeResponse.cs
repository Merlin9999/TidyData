 #nullable disable
 using NodaTime;

 namespace TidyData.Sync
{
    public class GetLastDeviceSyncTimeResponse
    {
        public Instant LastDeviceSyncTime { get; set; }
    }
}