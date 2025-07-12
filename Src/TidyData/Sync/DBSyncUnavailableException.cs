 #nullable disable
 using System.Net;

 namespace TidyData.Sync
{
    [Serializable]
    public class DBSyncUnavailableException : Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public DBSyncUnavailableException()
            : base("Sync Service reports that it is unavailable.")
        {
            this.Reason = EReason.ServiceReportsItsUnavailable;
        }

        public DBSyncUnavailableException(Exception exception)
            : base("Sync Service is unavailable. Either Internet access is unavailable or the service is inaccessible.", exception)
        {
            this.Reason = EReason.NetworkConnectivityError;
            this.HttpStatusErrorCode = null;
        }

        public DBSyncUnavailableException(DBSyncHttpStatusErrorException se)
            : base("Sync Service is unavailable. Either Internet access is unavailable or the service is inaccessible.", se)
        {
            this.Reason = EReason.NetworkConnectivityError;
            this.HttpStatusErrorCode = se.HttpStatusErrorCode;
        }

        public EReason Reason { get;}
        public HttpStatusCode? HttpStatusErrorCode { get; }

        public enum EReason
        {
            ServiceReportsItsUnavailable,
            NetworkConnectivityError,
        }
    }
}