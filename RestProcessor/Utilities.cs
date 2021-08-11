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
                if (orgInfo == null || orgInfo.Services == null)
                {
                    continue;
                }

                foreach (var service in orgInfo.Services)
                {
                    if (service == null || service.SwaggerInfo == null)
                    {
                        continue;
                    }

                    foreach (var swagger in service.SwaggerInfo)
                    {
                        if (swagger == null || swagger.Source != null)
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
