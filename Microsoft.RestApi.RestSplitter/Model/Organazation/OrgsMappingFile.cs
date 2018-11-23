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
        public bool UseYamlSchema { get; set; } = true;

        [JsonProperty("convert_yaml_to_json")]
        public bool ConvertYamlToJson { get; set; } = true;

        [JsonProperty("remove_tag_from_operationId")]
        public bool RemoveTagFromOperationId { get; set; }

        [JsonProperty("need_resolve_x_ms_paths")]
        public bool NeedResolveXMsPaths { get; set; } = true;

        [JsonProperty("version_list")]
        public List<string> VersionList { get; set; }

        [JsonProperty("uid_product_name")]
        public string ProductUid { get; set; }

        [JsonProperty("use_service_url_group")]
        public bool UseServiceUrlGroup { get; set; } = true;

        [JsonProperty("formalize_url")]
        public bool FormalizeUrl { get; set; }

        [JsonProperty("use_yaml_toc")]
        public bool UserYamlToc { get; set; }

        [JsonProperty("generate_source_url")]
        public bool GenerateSourceUrl { get; set; }

        [JsonProperty("no_split_words")]
        public List<string> NoSplitWords { get; set; }
    }
}
