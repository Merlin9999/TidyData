 #nullable disable
  namespace TidyData
{
    public sealed record DocumentMetaData
    {
        public bool Deleted { get; init; }
    }

    public static class DocumentMetaDataExtensions
    {
        public static DocumentMetaData AsDeleted(this DocumentMetaData cloneFrom, bool deleted = true)
        {
            if (cloneFrom == null)
                return new DocumentMetaData() { Deleted = deleted };

            return cloneFrom with { Deleted = deleted };
        }
    }
}