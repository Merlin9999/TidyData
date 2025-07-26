 #nullable disable
 using System.Collections.Immutable;
 using NodaTime;

 namespace TidyData.Query
{
    public record DeviceGetLastSyncTimeQuery<TServerDataModel> : IQuery<TServerDataModel, Instant>
        where TServerDataModel : ISyncServerDataModel
    {
        private readonly Guid _deviceId;

        public DeviceGetLastSyncTimeQuery(Guid deviceId)
        {
            this._deviceId = deviceId;
        }

        public Instant Execute(TServerDataModel model, CollectionWrapperFactory factory)
        {
            ImmutableDictionary<Guid, DeviceInformation> modelDevices = model.RemoteDeviceLookup 
                ?? ImmutableDictionary<Guid, DeviceInformation>.Empty;

            if (modelDevices.TryGetValue(this._deviceId, out DeviceInformation deviceInformation))
                return deviceInformation.LastSyncTimeStamp;
            return Instant.MinValue;
        }
    }
}