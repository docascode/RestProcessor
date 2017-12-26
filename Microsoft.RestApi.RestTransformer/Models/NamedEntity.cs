namespace Microsoft.RestApi.RestTransformer.Models
{
    using System;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    [Serializable]

    public class NamedEntity : IdentifiableEntity
    {
        [YamlMember(Alias = "name", Order = -9)]
        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
