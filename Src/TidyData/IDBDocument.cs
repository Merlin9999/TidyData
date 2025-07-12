 #nullable disable
  namespace TidyData
{
    public interface IDBDocument : IHasId
    {
        DocumentVersion Version { get; init; }
        DocumentMetaData Meta { get; init; }
    }
}