namespace Microsoft.RestApi.RestTransformer
{
    using System;

    using YamlDotNet.Core;
    using YamlDotNet.Serialization;

    public class StringConverter : IYamlTypeConverter
    {
        public bool Accepts(Type type)
        {
            return type == typeof(string);
        }

        public object ReadYaml(IParser parser, Type type)
        {
            throw new NotImplementedException();
        }

        public void WriteYaml(IEmitter emitter, object value, Type type)
        {
        }
    }
}
