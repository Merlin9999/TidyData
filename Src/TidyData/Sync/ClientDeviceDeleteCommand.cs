 #nullable disable
  namespace TidyData.Sync;

public class ClientDeviceDeleteCommand<TServerDataModel> : IAsyncCommand<TServerDataModel>
    where TServerDataModel : ISyncServerDataModel, new()
{
    private readonly Guid _remoteDeviceIdToDelete;

    public ClientDeviceDeleteCommand(Guid remoteDeviceIdToDelete)
    {
        this._remoteDeviceIdToDelete = remoteDeviceIdToDelete;
    }

    public Task ExecuteAsync(TServerDataModel model, CollectionWrapperFactory factory)
    {
        model.RemoteDeviceLookup = model.RemoteDeviceLookup.Remove(this._remoteDeviceIdToDelete);
        return Task.CompletedTask;
    }
}