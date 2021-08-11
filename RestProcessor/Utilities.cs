using Microsoft.RestApi.RestSplitter.Model;
using System.Collections.Generic;
using System.IO;

namespace RestProcessorUtilities
{
    public class Utilities
    {
        public static List<string> ExtractFilePath(string rootPath,OrgsMappingFile mappingFile)
        {
            if (mappingFile == null || mappingFile.OrgInfos==null)
            {
                return null;
            }

            var result = new List<string>();
            foreach (var info in mappingFile.OrgInfos)
            {
                foreach (var service in info.Services)
                {
                    foreach (var swagger in service.SwaggerInfo)
                    {
                        result.Add(Path.Combine(rootPath, swagger.Source));
                    }
                }
            }

            return result;
        }
    }
}
