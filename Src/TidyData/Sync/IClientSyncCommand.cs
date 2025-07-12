 #nullable disable
namespace TidyData.Sync
{
    /// <summary>
    /// Marker interface to indicate that this database command is part of the server synchronization process and
    /// should NOT trigger another database sync.
    /// </summary>
    public interface IClientSyncCommand { }
}