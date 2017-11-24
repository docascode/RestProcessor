namespace Microsoft.RestApi.RestTransformer.Models
{
    using YamlDotNet.Serialization;

    public class PathEntity
    {
        [YamlMember(Alias = "content")]
        public string Content { get; set; }

        [YamlMember(Alias = "isOptional")]
        public bool IsOptional { get; set; }
    }
}
