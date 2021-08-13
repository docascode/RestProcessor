namespace Microsoft.RestApi.SwaggerResolver.Core.Logging
{
    using YamlDotNet.RepresentationModel;

    public class ObjectPathPartIndex : ObjectPathPart
    {
        public ObjectPathPartIndex(int index)
        {
            Index = index;
        }

        public int Index { get; }

        public override string JsonPointer => $"/{Index + 1}";

        public override string JsonPath => $"[{Index + 1}]";

        public override string ReadablePath => JsonPath;

        public override YamlNode SelectNode(ref YamlNode node)
        {
            var snode = node as YamlSequenceNode;
            node = snode != null && 0 <= Index && Index < snode.Children.Count
                ? snode.Children[Index]
                : null;
            return node;
        }
    }
}
