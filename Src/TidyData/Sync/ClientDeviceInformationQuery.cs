 #nullable disable
 using System.Collections.Immutable;

 namespace TidyData.Sync
{
    public class ClientDeviceInformationQuery<TServerDataModel> : IAsyncQuery<TServerDataModel, ImmutableList<DeviceInformation>>
        where TServerDataModel : ISyncServerDataModel, new()
    {
        public Task<ImmutableList<DeviceInformation>> ExecuteAsync(TServerDataModel model, CollectionWrapperFactory factory)
        {
            return Task.FromResult(model.RemoteDeviceLookup.Values.ToImmutableList());
        }
    }
}
