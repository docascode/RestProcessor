namespace Microsoft.RestApi.RestTransformer.Models
{
    using System.Collections.Generic;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public class DefinitionObject
    {
        public DefinitionObject()
        {
            PropertyItems = new List<DefinitionObject>();
            AllOfs = new List<DefinitionObject>();
        }

        public string Name { get; set; }

        public string Title { get; set; }

        public string SubTitle { get; set; }

        public string Description { get; set; }

        public string SubDescription { get; set; }

        public string Type { get; set; }

        public string AdditionalType { get; set; }

        public bool IsReadOnly { get; set; }

        public bool IsRequired { get; set; }

        public bool IsFlatten { get; set; }

        public string DiscriminatorKey { get; set; }

        public string DiscriminatorValue { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public DefinitionObjectType DefinitionObjectType { get; set; }

        public string Pattern { get; set; }

        public string Format { get; set; }

        public IList<EnumValue> EnumValues { get; set; }

        public IList<DefinitionObject> PropertyItems { get; set; }

        public IList<DefinitionObject> AllOfs { get; set; }
    }

    public class EnumValue
    {
        public string Name { get; set; }

        public string Value { get; set; }

        public string Description { get; set; }
    }

    public enum DefinitionObjectType
    {
        Array,
        Object,
        Enum,
        Simple
    }

    public class Definition
    {
        public string Name { get; set; }

        public string Title { get; set; }

        public string SubTitle { get; set; }

        public string Description { get; set; }

        public string SubDescription { get; set; }

        public string Type { get; set; }

        public DefinitionObjectType DefinitionObjectType { get; set; }

        public IList<string> AllOfTypes { get; set; }

        public IList<DefinitionProperty> DefinitionProperties { get; set; }

        public IList<EnumValue> EnumValues { get; set; }

        public string DiscriminatorValue { get; set; }

        public string DiscriminatorKey { get; set; }
    }

    public class DefinitionProperty
    {
        public string Name { get; set; }

        public string Title { get; set; }

        public string SubTitle { get; set; }

        public string Description { get; set; }

        public string SubDescription { get; set; }

        public string Type { get; set; }

        public string AdditionalType { get; set; }

        public bool IsReadOnly { get; set; }

        public bool IsRequired { get; set; }

        public bool IsFlatten { get; set; }

        public string DiscriminatorKey { get; set; }

        public string DiscriminatorValue { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public DefinitionObjectType DefinitionObjectType { get; set; }

        public IList<EnumValue> EnumValues { get; set; }

        public string Pattern { get; set; }

        public string Format { get; set; }
    }
}
