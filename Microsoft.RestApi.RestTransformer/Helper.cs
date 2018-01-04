namespace Microsoft.RestApi.RestTransformer
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Microsoft.RestApi.RestTransformer.Models;

    public static class Helper
    {
        public static string GetOptionalFullPath(IList<PathEntity> paths)
        {
            var pathContent = paths.Where(p => p.IsOptional).FirstOrDefault()?.Content;
            if (string.IsNullOrEmpty(pathContent))
            {
                pathContent = paths.First().Content;
            }
            return pathContent;
        }

        public static IList<ParameterEntity> SortParameters(IList<PathEntity> paths, IList<ParameterEntity> parameters)
        {
            var sortedParameters = new List<ParameterEntity>();
            var pathContent = GetOptionalFullPath(paths);
            var regex = new Regex("{.*?}");
            var matchResults = regex.Matches(pathContent);
            for (int i = 0; i < matchResults.Count; i++)
            {
                var parameter = parameters.FirstOrDefault(p => $"{{{p.Name}}}" == matchResults[i].Value);
                if (parameter != null)
                {
                    sortedParameters.Add(parameter);
                }
            }
            return sortedParameters;
        }

        public static IList<PathEntity> HandlePathsDefaultValues(IList<PathEntity> paths, string apiVersion, IList<ParameterEntity> parameters)
        {
            foreach (var path in paths)
            {
                path.Content = path.Content.Replace("{api-version}", apiVersion);
                foreach (var parameter in parameters)
                {
                    if(parameter.EnumValues?.Count() == 1)
                    {
                        path.Content = path.Content.Replace($"{{{parameter.Name}}}", parameter.EnumValues[0]);
                    }
                   
                }
            }
            
            return paths;
        }
    }
}
