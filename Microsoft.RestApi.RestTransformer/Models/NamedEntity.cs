namespace Microsoft.RestApi.RestTransformer.Models
{
    using System;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    [Serializable]

    public class NamedEntity : IdentifiableEntity
    {
        [YamlMember(Alias = "name")]
        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
