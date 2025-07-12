 #nullable disable
  namespace TidyData.Sync;

[Serializable]
public class DBSyncVersionMismatchException : Exception
{
    //
    // For guidelines regarding the creation of new exception types, see
    //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
    // and
    //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
    //

    public DBSyncVersionMismatchException(string clientVersion, string serverVersion)
        : base(BuildErrorMessage(clientVersion, serverVersion))
    {
    }

    private static string BuildErrorMessage(string clientVersion, string serverVersion)
    {
        return $"Sync Version Mismatch.\nClient Version: {clientVersion}\nServer Version: {serverVersion}";
    }
}