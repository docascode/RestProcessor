namespace RestProcessor
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;

    [Serializable]
    public class OrgsMappingFile
    {
        [JsonProperty("target_api_root_dir")]
        public string TargetApiRootDir { get; set; }

        [JsonProperty("apis_page_options")]
        public ApisPageOptions ApisPageOptions { get; set; }

        [JsonProperty("organizations")]
        public List<OrgInfo> OrgInfos { get; set; }
    }
}
