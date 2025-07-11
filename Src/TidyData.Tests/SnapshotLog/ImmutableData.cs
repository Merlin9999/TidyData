 #nullable disable
 using Newtonsoft.Json;

 namespace TidyData.Tests.SnapshotLog
{
    public class ImmutableData
    {
        public readonly string Name;
        public readonly ulong Count;
        public readonly ImmutableObject NestedImmutableObject;

        [JsonConstructor]
        public ImmutableData(string name = null, ulong? count = null, ImmutableObject nestedImmutableObject = null)
            : this((ImmutableData)null, name, count, nestedImmutableObject)
        {
        }

        public ImmutableData(ImmutableData origModel, string name = null, ulong? count = null, ImmutableObject nestedImmutableObject = null)
        {
            this.Name = name ?? origModel?.Name ?? String.Empty;
            this.Count = count ?? origModel?.Count ?? 0;
            this.NestedImmutableObject = nestedImmutableObject ?? origModel?.NestedImmutableObject;
        }

        public ImmutableData()
            : this((ImmutableData)null)
        {
        }
    }

    public class ImmutableObject
    {
        public readonly string Value;

        [JsonConstructor]
        public ImmutableObject(string value)
            : this((ImmutableObject)null, value)
        {
        }

        public ImmutableObject(ImmutableObject origModel, string value = null)
        {
            this.Value = value ?? origModel?.Value ?? string.Empty;
        }
    }
}