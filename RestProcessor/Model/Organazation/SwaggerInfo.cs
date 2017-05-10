namespace RestProcessor
{
    using System;

    using Newtonsoft.Json;

    [Serializable]
    public class SwaggerInfo
    {
        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("operation_group_mapping")]
        public OperationGroupMapping OperationGroupMapping { get; set; }
    }
}
