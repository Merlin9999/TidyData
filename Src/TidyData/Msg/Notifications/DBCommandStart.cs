 #nullable disable
 using TidyMediator;

 namespace TidyData.Msg.Notifications
{
    public static class DBCommandStart
    {
        public class Event : INotification
        {
            public Guid CommandId { get; set; }
            public Guid AccountId { get; set; }
            public bool IsClientSyncCommand { get; set; }
            public Type CommandType { get; set; }
        }
    }
}