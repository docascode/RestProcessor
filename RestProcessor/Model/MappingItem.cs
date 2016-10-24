namespace RestProcessor
{
    using System;

    using Newtonsoft.Json;

    [Serializable]
    public class MappingItem
    {
        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("target_dir")]
        public string TargetDir { get; set; }

        [JsonProperty("toc_title")]
        public string TocTitle { get; set; }
    }
}
