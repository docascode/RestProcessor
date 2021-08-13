namespace Microsoft.RestApi.SwaggerResolver.Core.Logging
{
    using YamlDotNet.RepresentationModel;

    public abstract class ObjectPathPart
    {
        public abstract string JsonPointer { get; }

        public abstract string JsonPath { get; }

        public abstract string ReadablePath { get; }

        /// <summary>
        /// Selects the child node according to this path part.
        /// Returns null if such node was not found.
        /// </summary>
        public abstract YamlNode SelectNode(ref YamlNode node);
    }
}
