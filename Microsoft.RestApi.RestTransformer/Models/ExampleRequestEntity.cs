namespace Microsoft.RestApi.RestTransformer.Models
{
    using System.Collections.Generic;

    using YamlDotNet.Serialization;

    public class ExampleRequestEntity
    {
        [YamlMember(Alias = "uri")]
        public string RequestUri { get; set; }

        [YamlMember(Alias = "body")]
        public string RequestBody { get; set; }

        [YamlMember(Alias = "headers")]
        public IList<ExampleRequestHeaderEntity> Headers { get; set; }
    }
}
