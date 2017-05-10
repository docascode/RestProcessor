namespace RestProcessor
{
    using System;

    using Newtonsoft.Json;

    [Serializable]
    public class MappingFile
    {
        [JsonProperty("target_api_root_dir")]
        public string TargetApiRootDir { get; set; }

        [JsonProperty("mapping")]
        public Mapping Mapping { get; set; }
    }
}
