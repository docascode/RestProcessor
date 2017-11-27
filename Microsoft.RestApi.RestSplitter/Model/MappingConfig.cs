namespace Microsoft.RestApi.RestSplitter.Model
{
    public class MappingConfig
    {
        public bool IsOperationLevel { get; set; }

        public bool IsGroupedByTag { get; set; }

        public int SplitOperationCountGreaterThan { get; set; }

        public bool UseYamlSchema { get; set; }
    }
}
