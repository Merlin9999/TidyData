 #nullable disable
namespace TidyData.Sync
{
    public class GetSyncServiceStatusResponse
    {
        public bool IsAvailable { get; set; }
        public string Version { get; set; }
        public string InformationalVersion { get; set; }
    }
}