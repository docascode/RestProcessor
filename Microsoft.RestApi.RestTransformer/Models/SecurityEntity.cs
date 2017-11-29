namespace Microsoft.RestApi.RestTransformer.Models
{
    using System.Collections.Generic;

    using YamlDotNet.Serialization;

    public class SecurityEntity : NamedEntity
    {
        [YamlIgnore]
        public string Key { get; set; }

        [YamlMember(Alias = "type")]
        public string Type { get; set; }

        [YamlMember(Alias = "description")]
        public string Description { get; set; }

        [YamlMember(Alias = "in")]
        public string In { get; set; }

        [YamlMember(Alias = "flow")]
        public string Flow { get; set; }

        [YamlMember(Alias = "authorizationUrl")]
        public string AuthorizationUrl { get; set; }

        [YamlMember(Alias = "tokenUrl")]
        public string TokenUrl { get; set; }

        [YamlMember(Alias = "scopes")]
        public IList<SecurityScopeEntity> Scopes { get; set; }
    }

    public class SecurityScopeEntity : NamedEntity
    {
        [YamlMember(Alias = "description")]
        public string Description { get; set; }
    }
}
