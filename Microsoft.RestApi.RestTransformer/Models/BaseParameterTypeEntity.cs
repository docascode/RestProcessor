namespace Microsoft.RestApi.RestTransformer.Models
{
    using System.Collections.Generic;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    public class BaseParameterTypeEntity : IdentifiableEntity
    {
        [YamlMember(Alias = "isArray")]
        [JsonProperty("isArray")]
        public bool IsArray { get; set; } = false;

        [YamlMember(Alias = "isDictionary")]
        [JsonProperty("isDictionary")]
        public bool IsDictionary { get; set; } = false;

        [YamlMember(Alias = "additionalTypes")]
        [JsonProperty("additionalTypes")]
        public List<IdentifiableEntity> AdditionalTypes { get; set; }
    }
}
