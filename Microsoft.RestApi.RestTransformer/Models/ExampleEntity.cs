namespace Microsoft.RestApi.RestTransformer.Models
{
    using System.Collections.Generic;

    using YamlDotNet.Serialization;

    public class ExampleEntity : NamedEntity
    {
        [YamlMember(Alias = "request")]
        public string Request { get; set; }

        [YamlMember(Alias = "requestBody")]
        public string RequestBody { get; set; }

        [YamlMember(Alias = "responses")]
        public IList<ExampleResponseEntity> ExampleResponses { get; set; }
    }
}
