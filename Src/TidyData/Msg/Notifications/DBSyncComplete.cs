 #nullable disable
 using TidyMediator;

 namespace TidyData.Msg.Notifications;

public static class DBSyncComplete
{
    public record Event : INotification
    {
        public Guid AccountId { get; init; }
        public bool IncludedChangesFromServer { get; init; }
    }
}
