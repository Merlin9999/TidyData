 #nullable disable
  namespace TidyData.Sync
{
    public interface IDBClientSync : IDBSyncServiceAPI
    {
        Task<GetSyncServiceStatusResponse> GetSyncServiceStatus(GetSyncServiceStatusRequest request);
        Task<GetLastDeviceSyncTimeResponse> GetLastDeviceSyncTimeAsync(GetLastDeviceSyncTimeRequest request);
        Task<SynchronizeResponse> SynchronizeAsync(SynchronizeRequest clientChangeSet);
    }

    public interface IDBSyncServiceAPI
    {
        Task<ListRegisteredDevicesResponse> ListRegisteredDevicesAsync(ListRegisteredDevicesRequest request);
        Task<DeleteRegisteredDeviceResponse> DeleteRegisteredDeviceAsync(DeleteRegisteredDeviceRequest request);
    }
}