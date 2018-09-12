namespace Microsoft.RestApi.RestSplitter.Model
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;

    [Serializable]
    public class RepoFile
    {
        [JsonProperty("repo")]
        public IList<RepoConfig> Repos { get; set; }
    }

    public class RepoConfig
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("branch")]
        public string Branch { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("enable_view_code")]
        public bool EnableViewCode { get; set; }
    }
}
