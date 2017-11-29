namespace Microsoft.RestApi.RestTransformer.Models
{
    using System.Collections.Generic;

    using YamlDotNet.Serialization;

    public class DefinitionEntity
    {
        [YamlMember(Alias = "name")]
        public string Name { get; set; }

        [YamlMember(Alias = "description")]
        public string Description { get; set; }

        [YamlMember(Alias = "kind")]
        public string Kind { get; set; }

        [YamlMember(Alias = "properties")]
        public IList<DefinitionParameterEntity> ParameterItems { get; set; }

        [YamlIgnore]
        public List<string> AllOfTypes { get; set; }
    }

    public class DefinitionParameterEntity: BaseParameterEntity
    {
    }
}
