namespace Microsoft.RestApi.RestTransformer.Models
{
    using System.Collections.Generic;

    using YamlDotNet.Serialization;

    public class OperationEntity : NamedEntity
    {
        [YamlMember(Alias = "service", Order = -8)]
        public string Service { get; set; }

        [YamlIgnore]
        public string GroupId { get; set; }

        [YamlMember(Alias = "groupName", Order = -7)]
        public string GroupName { get; set; }

        [YamlMember(Alias = "apiVersion", Order = -6)]
        public string ApiVersion { get; set; }

        [YamlMember(Alias = "summary")]
        public string Summary { get; set; }

        [YamlMember(Alias = "remarks")]
        public string Remarks { get; set; }

        [YamlMember(Alias = "isDeprecated")]
        public bool IsDeprecated { get; set; } = false;

        [YamlMember(Alias = "isPreview")]
        public bool IsPreview { get; set; } = false;

        [YamlMember(Alias = "consumes")]
        public string[] Consumes { get; set; }

        [YamlMember(Alias = "produces")]
        public string[] Produces { get; set; }

        [YamlMember(Alias = "paths")]
        public IList<PathEntity> Paths { get; set; }

        [YamlMember(Alias = "uriParameters")]
        public IList<ParameterEntity> Parameters { get; set; }

        [YamlMember(Alias = "responses")]
        public IList<ResponseEntity> Responses { get; set; }

        [YamlMember(Alias = "requestBody")]
        public IList<RequestBody> RequestBodies { get; set; }

        [YamlMember(Alias = "requestHeader")]
        public IList<ParameterEntity> RequestHeaders { get; set; }

        [YamlMember(Alias = "definitions")]
        public IList<DefinitionEntity> Definitions { get; set; }

        [YamlMember(Alias = "examples")]
        public IList<ExampleEntity> Examples { get; set; }

        [YamlMember(Alias = "security")]
        public IList<SecurityEntity> Securities { get; set; }

        [YamlMember(Alias = "metadata")]
        public MetaDataEntity Metadata { get; set; }

        [YamlMember(Alias = "error")]
        public IList<ErrorCodeEntity> ErrorCodes { get; set; }
    }
}
