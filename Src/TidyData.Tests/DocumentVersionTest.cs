 #nullable disable
using FluentAssertions;
using TidyData;
using TidyUtility.Data.Json;
using Xunit;

namespace TidySyncDB.UnitTests
{
    public class DocumentVersionTest
    {
        private readonly ISerializer _serializer = new SafeJsonDotNetSerializer();

        [Fact]
        public void ClonesAreEqual()
        {
            DocumentVersion version1 = new DocumentVersion();
            DocumentVersion version2 = version1 with { };

            version1.Should().NotBeSameAs(version2);
            version1.Should().Be(version2);
        }

        [Fact]
        public void NonClonesAreNotEqual()
        {
            DocumentVersion version1 = new DocumentVersion();
            DocumentVersion version2 = new DocumentVersion();

            version1.Should().NotBeSameAs(version2);
        }

        [Fact]
        public void NullIsNotEqual()
        {
            DocumentVersion version = new DocumentVersion();

            version.Should().NotBe(null);
        }

        [Fact]
        public void SameInstantIsEqual()
        {
            DocumentVersion version = new DocumentVersion();

            version.Should().Be(version);
        }

        [Fact]
        public void SerializedThenDeserializedAreEqual()
        {
            DocumentVersion metaDataBefore = new DocumentVersion();
            string serialized = this._serializer.Serialize(metaDataBefore);
            DocumentVersion metaDataAfter = this._serializer.Deserialize<DocumentVersion>(serialized);

            metaDataBefore.Should().Be(metaDataAfter);
        }
    }
}