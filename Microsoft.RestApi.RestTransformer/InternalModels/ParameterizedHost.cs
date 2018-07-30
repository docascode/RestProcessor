using Microsoft.RestApi.RestTransformer.Models;
using System.Collections.Generic;

namespace Microsoft.RestApi.RestTransformer
{
    public class ParameterizedHost
    {
        public string Host { get; set; }

        public bool UseSchemePrefix { get; set; }

        public List<ParameterEntity> Parameters { get; set; }
    }
}
