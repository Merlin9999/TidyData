 #nullable disable
  namespace TidyData.Storage
{
    [Serializable]
    public class StorageConcurrencyException : Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public StorageConcurrencyException()
            : base("Attempt to access the same storage medium while already being accessed. Wait a few moments and retry.")
        {
        }

        public StorageConcurrencyException(Exception inner)
            : base("Attempt to access the same storage medium while already being accessed. Wait a few moments and retry.", inner)
        {

        }

        public StorageConcurrencyException(string message) 
            : base(message)
        {
        }

        public StorageConcurrencyException(string message, Exception inner) 
            : base(message, inner)
        {
        }
    }
}
