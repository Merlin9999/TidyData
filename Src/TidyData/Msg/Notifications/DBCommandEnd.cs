 #nullable disable
 using TidyMediator;

 namespace TidyData.Msg.Notifications
{
    public static class DBCommandEnd
    {
        public class Event : INotification
        {
            public Guid CommandId { get; set; }
            public Guid AccountId { get; set; }
            public bool IsClientSyncCommand { get; set; }
            public Type CommandType { get; set; }
            public bool Success { get => this.Exception == null; }
            public Exception Exception { get; set; }
        }
    }
}