namespace Microsoft.RestApi.RestTransformer.Models
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    [Serializable]
    public class ResponseEntity : BaseParameterEntity
    {
        [YamlMember(Alias = "headers")]
        [JsonProperty("headers")]
        public IList<ResponseHeader> ResponseHeaders { get; set; }
    }

    public class ResponseHeader
    {
        [YamlMember(Alias = "name")]
        [JsonProperty("name")]
        public string Name { get; set; }

        [YamlMember(Alias = "value")]
        [JsonProperty("value")]
        public string Value { get; set; }
    }
}
