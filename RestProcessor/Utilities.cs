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
            foreach (var orgInfo in mappingFile.OrgInfos)
            {
                if (orgInfo == null)
                {
                    continue;
                }

                foreach (var service in orgInfo.Services)
                {
                    if (service == null)
                    {
                        continue;
                    }

                    foreach (var swagger in service.SwaggerInfo)
                    {
                        if (swagger.Source != null)
                        {
                            result.Add(Path.Combine(rootPath, swagger.Source));
                        }
                    }
                }
            }

            return result;
        }
    }
}
