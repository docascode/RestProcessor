namespace Microsoft.RestApi.RestTransformer.Models
{
    using YamlDotNet.Serialization;

    public class MetaDataEntity
    {
        [YamlMember(Alias = "source_url")]
        public string SourceUrl { get; set; }
    }
}