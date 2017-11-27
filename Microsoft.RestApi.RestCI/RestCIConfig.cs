
namespace Microsoft.RestApi.RestCI
{
    using System;

    using Newtonsoft.Json;

    [Serializable]
    public class RestCIConfig
    {
        [JsonProperty("use_yaml_schema")]
        public bool UseYamlSchema { get; set; } = true;
    }
}
