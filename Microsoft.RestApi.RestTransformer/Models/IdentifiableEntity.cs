namespace Microsoft.RestApi.RestTransformer.Models
{
    using System;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    [Serializable]
    public class IdentifiableEntity
    {
        [YamlMember(Alias = "uid")]
        [JsonProperty("id")]
        public string Id { get; set; }
    }
}
