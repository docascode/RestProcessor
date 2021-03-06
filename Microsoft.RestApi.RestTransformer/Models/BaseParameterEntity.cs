﻿namespace Microsoft.RestApi.RestTransformer.Models
{
    using System.Collections.Generic;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    public class BaseParameterEntity : NamedEntity
    {
        [YamlMember(Alias = "isReadyOnly")]
        [JsonProperty("readyOnly")]
        public bool IsReadOnly { get; set; }

        [YamlMember(Alias = "description")]
        [JsonProperty("description")]
        public string Description { get; set; }

        [YamlMember(Alias = "types")]
        [JsonProperty("types")]
        public List<BaseParameterTypeEntity> Types { get; set; }

        [YamlMember(Alias = "typesTitle")]
        public string TypesTitle { get; set; }

        [YamlMember(Alias = "pattern")]
        [JsonProperty("pattern")]
        public string Pattern { get; set; }

        [YamlMember(Alias = "format")]
        [JsonProperty("format")]
        public string Format { get; set; }
    }
}
