namespace RestProcessor
{
    using System;

    using Newtonsoft.Json;

    [Serializable]
    public class ReferenceItem
    {
        [JsonProperty("operation_group_mapping")]
        public OperationGroupMapping OperationGroupMapping { get; set; }

        [JsonProperty("source_swagger")]
        public string SourceSwagger { get; set; }

        [JsonProperty("target_dir")]
        public string TargetDir { get; set; }

        [JsonProperty("toc_title")]
        public string TocTitle { get; set; }
    }
}
