namespace RestProcessor
{
    using System.Linq;

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
                    foreach (var item in xMsPath.Values())
                    {
                        var itemValue = item.Values<JObject>().FirstOrDefault();
                        if (itemValue != null)
                        {
                            itemValue["x-internal-path-from"] = DefinitionKey;
                        }
                    }
                    pathObjects.Add(xMsPath);
                }
                _root["paths"] = pathObjects;
                _root.Remove(DefinitionKey);
            }
        }
    }
}
