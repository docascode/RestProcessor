namespace Microsoft.RestApi.RestTransformer.Models
{
    using System;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    [Serializable]
    public class ParameterEntity : BaseParameterEntity
    {
        [YamlMember(Alias = "in")]
        [JsonProperty("in")]
        public string In { get; set; }

        [YamlMember(Alias = "isRequired")]
        [JsonProperty("required")]
        public bool IsRequired { get; set; }

        [YamlMember(Alias = "skipUrlEncoding")]
        [JsonProperty("x-ms-skip-url-encoding")]
        public bool SkipUrlEncoding { get; set; }

        [YamlIgnore]
        public string[] EnumValues { get; set; }

        [YamlIgnore]
        public ParameterEntityType ParameterEntityType { get; set; }
    }

    public enum ParameterEntityType
    {
        Query,
        Path,
        Header,
        Body
    }
}
