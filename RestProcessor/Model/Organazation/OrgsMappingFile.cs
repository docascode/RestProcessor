namespace RestProcessor
{
    using System;

    using Newtonsoft.Json;

    [Serializable]
    public class OrgsMappingFile
    {
        [JsonProperty("organizations")]
        public OrgsMapping OrgsMapping { get; set; }
    }
}
