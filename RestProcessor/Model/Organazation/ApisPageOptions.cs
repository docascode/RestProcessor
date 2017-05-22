namespace RestProcessor
{
    using System;

    using Newtonsoft.Json;

    [Serializable]
    public class ApisPageOptions
    {
        [JsonProperty("enable_auto_generate")]
        public bool EnableAutoGenerate { get; set; }

        [JsonProperty("target_file")]
        public string TargetFile { get; set; }

        [JsonProperty("summary_file")]
        public string SummaryFile { get; set; }

        [JsonProperty("service_description_metadata")]
        public string ServiceDescriptionMetadata { get; set; }
    }
}
