namespace Microsoft.RestApi.RestSplitter.Model
{
    using System;

    using Newtonsoft.Json;

    [Serializable]
    public class SwaggerInfo
    {
        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("needPermission")]
        public bool NeedPermission { get; set; }

        [JsonProperty("sub_group_toc_title")]
        public string SubGroupTocTitle { get; set; }

        [JsonProperty("operation_group_mapping")]
        public OperationGroupMapping OperationGroupMapping { get; set; }
    }
}
