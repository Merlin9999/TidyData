 #nullable disable
 using TidyMediator;

 namespace TidyData.Msg.Notifications
{
    public static class DBCommandEnd
    {
        public record Event : INotification
        {
            public Guid CommandId { get; init; }
            public Guid AccountId { get; init; }
            public bool IsClientSyncCommand { get; init; }
            public Type CommandType { get; init; }
            public bool Success => this.Exception == null;
            public Exception Exception { get; init; }
        }
    }
}