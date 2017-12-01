namespace Microsoft.RestApi.RestTransformer.Models
{
    using System.Collections.Generic;

    using YamlDotNet.Serialization;

    public class ExampleEntity : NamedEntity
    {
        [YamlMember(Alias = "request")]
        public ExampleRequestEntity ExampleRequest { get; set; }

        [YamlMember(Alias = "responses")]
        public IList<ExampleResponseEntity> ExampleResponses { get; set; }
    }
}
