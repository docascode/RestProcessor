namespace RestProcessor
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;

    [Serializable]
    public class OrgInfo
    {
        [JsonProperty("name")]
        public string OrgName { get; set; }

        [JsonProperty("index")]
        public string OrgIndex { get; set; }

        [JsonProperty("default_toc_title")]
        public string DefaultTocTitle { get; set; }

        [JsonProperty("services")]
        public List<ServiceInfo> Services { get; set; }
    }
}
