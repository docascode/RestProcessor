namespace Microsoft.RestApi.RestTransformer.Models
{
    using System.Collections.Generic;

    using YamlDotNet.Serialization;

    public class OperationEntity : NamedEntity
    {
        [YamlMember(Alias = "summary")]
        public string Summary { get; set; }

        [YamlMember(Alias = "service")]
        public string Service { get; set; }

        [YamlMember(Alias = "apiVersion")]
        public string ApiVersion { get; set; }

        [YamlMember(Alias = "groupName")]
        public string GroupName { get; set; }

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
        public IList<ParameterEntity> RequestBodies { get; set; }

        [YamlMember(Alias = "requestHeader")]
        public IList<ParameterEntity> RequestHeaders { get; set; }

        [YamlMember(Alias = "definitions")]
        public IList<DefinitionEntity> Definitions { get; set; }

        [YamlMember(Alias = "examples")]
        public IList<ExampleEntity> Examples { get; set; }
    }
}
