 #nullable disable
 using FluentAssertions;
 using TidyUtility.Data.Json;

 namespace TidyData.Tests
{
    public class DocumentMetaDataTest
    {
        private readonly ISerializer _serializer = new SafeJsonDotNetSerializer();

        [Fact]
        public void ClonesAreEqual()
        {
            DocumentMetaData meta1 = new DocumentMetaData();
            DocumentMetaData meta2 = meta1 with { };

            meta1.Should().BeEquivalentTo(meta2);
        }

        [Fact]
        public void NonClonesAreEqual()
        {
            DocumentMetaData meta1 = new DocumentMetaData();
            DocumentMetaData meta2 = new DocumentMetaData();

            meta1.Should().BeEquivalentTo(meta2);
        }

        [Fact]
        public void NullIsNotEqual()
        {
            DocumentMetaData meta1 = new DocumentMetaData();

            meta1.Should().NotBeEquivalentTo<DocumentMetaData>(null);
        }

        [Fact]
        public void SameInstantIsEqual()
        {
            DocumentMetaData metaData = new DocumentMetaData();

            metaData.Should().BeEquivalentTo(metaData);
        }

        [Fact]
        public void SerializedThenDeserializedAreEqual()
        {
            DocumentMetaData metaDataBefore = new DocumentMetaData();
            string serialized = this._serializer.Serialize(metaDataBefore);
            DocumentMetaData metaDataAfter = this._serializer.Deserialize<DocumentMetaData>(serialized);

            metaDataBefore.Should().BeEquivalentTo(metaDataAfter);

        }
    }
}
