namespace Microsoft.RestApi.RestTransformer.Models
{

    using YamlDotNet.Serialization;

    public class ExampleResponseEntity
    {
        [YamlMember(Alias = "statusCode")]
        public string StatusCode { get; set; }

        [YamlMember(Alias = "headers")]
        public string Headers { get; set; }

        [YamlMember(Alias = "body")]
        public string Body { get; set; }
    }
}
