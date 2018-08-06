namespace Microsoft.RestApi.RestSplitter.Model
{
    using System;
    using System.Collections.Generic;

    [Serializable]
    public class OperationGroupMapping : Dictionary<string, string>
    {
        public bool TryGetValueOrDefault(string key, out string value, string defaultValue)
        {
            var flag = this.TryGetValue(key, out value);
            if (!flag)
            {
                value = defaultValue;
            }
            return flag;
        }
    }
}
