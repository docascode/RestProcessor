namespace Microsoft.RestApi.RestTransformer.Models
{
    using YamlDotNet.Serialization;

    public class ErrorCodeEntity : NamedEntity
    {
        [YamlMember(Alias = "code")]
        public string Code { get; set; }
    }
}
