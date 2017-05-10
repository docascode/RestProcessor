namespace RestProcessor
{
    using System;

    using Newtonsoft.Json;

    [Serializable]
    public class DocumentationItem
    {
        [JsonProperty("source_toc")]
        public string SourceToc { get; set; }

        [JsonProperty("source_index")]
        public string SourceIndex { get; set; }

        [JsonProperty("toc_title")]
        public string TocTitle { get; set; }
    }
}
