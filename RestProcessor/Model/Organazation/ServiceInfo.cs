namespace RestProcessor
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;

    [Serializable]
    public class ServiceInfo
    {
        [JsonProperty("toc_title")]
        public string TocTitle { get; set; }

        [JsonProperty("url_group")]
        public string url_group { get; set; }

        [JsonProperty("service_index")]
        public string Index { get; set; }

        [JsonProperty("service_toc")]
        public string Toc { get; set; }

        [JsonProperty("swagger_files")]
        public List<SwaggerInfo> SwaggerInfo { get; set; }
    }
}
