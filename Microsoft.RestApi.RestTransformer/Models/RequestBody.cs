namespace Microsoft.RestApi.RestTransformer.Models
{
    using System.Collections.Generic;

    using YamlDotNet.Serialization;

    public class RequestBody : NamedEntity
    {
        [YamlMember(Alias = "description")]
        public string Description { get; set; }

        [YamlMember(Alias = "parameters")]
        public IList<ParameterEntity> RequestBodyParameters { get; set; }
    }
}
