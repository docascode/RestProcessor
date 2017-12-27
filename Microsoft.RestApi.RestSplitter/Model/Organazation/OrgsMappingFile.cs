namespace Microsoft.RestApi.RestSplitter.Model
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

        [JsonProperty("is_operation_level")]
        public bool IsOperationLevel { get; set; } = true;

        [JsonProperty("is_grouped_by_tag")]
        public bool IsGroupdedByTag { get; set; }

        [JsonProperty("split_operation_count_greater_than")]
        public int SplitOperationCountGreaterThan { get; set; }

        [JsonProperty("use_yaml_schema")]
        public bool UseYamlSchema { get; set; } = false;

        [JsonProperty("convert_yaml_to_json")]
        public bool ConvertYamlToJson { get; set; } = true;

        [JsonProperty("remove_tag_from_operationId")]
        public bool RemoveTagFromOperationId { get; set; }
    }
}
