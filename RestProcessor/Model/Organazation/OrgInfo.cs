namespace RestProcessor
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;

    [Serializable]
    public class OrgInfo
    {
        [JsonProperty("organization_index")]
        public string OrgIndex { get; set; }

        [JsonProperty("services")]
        public List<ServiceInfo> Services { get; set; }
    }
}
