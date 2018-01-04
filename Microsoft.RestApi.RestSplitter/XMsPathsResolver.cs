namespace Microsoft.RestApi.RestSplitter
{
    using Newtonsoft.Json.Linq;

    public class XMsPathsResolver
    {
        private const string DefinitionKey = "x-ms-paths";
        private readonly JObject _root;

        public XMsPathsResolver(JObject root)
        {
            _root = root;
        }

        public void Resolve()
        {
            JToken xMsPaths;
            if (_root.TryGetValue(DefinitionKey, out xMsPaths))
            {
                var pathObjects = _root["paths"] as JObject ?? new JObject();
                foreach (var xMsPath in xMsPaths)
                {
                    pathObjects.Add(xMsPath);
                }
                _root["paths"] = pathObjects;
            }
        }
    }
}
