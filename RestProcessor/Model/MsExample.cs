namespace RestProcessor
{
    using System.Collections.Generic;

    using Newtonsoft.Json;

    using YamlDotNet.Serialization;

    public class MsExample
    {
        [YamlMember(Alias = "request")]
        [JsonProperty("request")]
        public string Request { get; set; }

        [YamlMember(Alias = "curl")]
        [JsonProperty("curl")]
        public string Curl { get; set; }

        [YamlMember(Alias = "response")]
        [JsonProperty("response")]
        public Dictionary<string, string> Response { get; set; }
    }
}
