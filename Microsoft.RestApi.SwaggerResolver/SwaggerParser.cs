namespace Microsoft.RestApi.SwaggerResolver
{
    using Microsoft.RestApi.SwaggerResolver.Core.Parsing;
    using Microsoft.RestApi.SwaggerResolver.Core.Utilities;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class SwaggerParser
    {
        public static string Resolver(string path)
        {
            var swaggerDocument = Settings.FileSystem.ReadAllText(path);
            return Parse(path, swaggerDocument);
        }
        public static string Parse(string path, string swaggerDocument)
        {
            try
            {
                swaggerDocument = Normalize(path, swaggerDocument);
            }
            catch (Exception ex)
            {
                throw ex;// ErrorManager.CreateError("{0}. {1}", Resources.ErrorParsingSpec, ex.Message);
            }

            return swaggerDocument;
        }
        public static string ResolveExternalReferencesInJson(this string path, string swaggerDocument)
        {
            JObject swaggerObject = JObject.Parse(swaggerDocument);
            var externalFiles = new Dictionary<string, JObject>();
            externalFiles[path] = swaggerObject;
            HashSet<string> visitedEntities = new HashSet<string>();
            EnsureCompleteDefinitionIsPresent(visitedEntities, externalFiles, path);
            EnsureCompleteExampleIsPresent(visitedEntities, externalFiles, path);
            SwaggerComposition.Travel(swaggerObject);
            return swaggerObject.ToString();
        }
        public static void EnsureCompleteDefinitionIsPresent(HashSet<string> visitedEntities, Dictionary<string, JObject> externalFiles, string sourceFilePath, string currentFilePath = null, string entityType = null, string modelName = null)
        {
            IEnumerable<JToken> references;
            var sourceDoc = externalFiles[sourceFilePath];
            if (currentFilePath == null)
            {
                currentFilePath = sourceFilePath;
            }

            var currentDoc = externalFiles[currentFilePath];
            if (entityType == null && modelName == null)
            {
                //first call to the recursive function. Hence we will process file references only.
                references = currentDoc.SelectTokens("$..$ref").Where(p => !((string)p).StartsWith("#") && !((string)p).Contains("example"));
            }
            else
            {
                //It is possible that the external doc had a fully defined model. Hence we need to process all the refs of that model.
                references = currentDoc[entityType][modelName].SelectTokens("$..$ref");
            }

            foreach (JValue value in references)
            {
                var path = (string)value;
                string[] splitReference = path.Split(new[] { '#' }, StringSplitOptions.RemoveEmptyEntries);
                string filePath = null, entityPath = path;
                if (path != null && splitReference.Length == 2)
                {
                    filePath = splitReference[0];
                    entityPath = "#" + splitReference[1];
                    value.Value = entityPath;
                    // Make sure the filePath is either an absolute uri, or a rooted path
                    if (!Settings.FileSystem.IsCompletePath(filePath))
                    {
                        // Otherwise, root it from the directory (one level up) of the current swagger file path
                        filePath = Settings.FileSystem.MakePathRooted(Settings.FileSystem.GetParentDir(currentFilePath), filePath);
                    }
                    if (!externalFiles.ContainsKey(filePath))
                    {
                        var externalDefinitionString = Settings.FileSystem.ReadAllText(filePath);
                        externalFiles[filePath] = JObject.Parse(externalDefinitionString);
                    }
                }

                var referencedEntityType = entityPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)[1];
                var referencedModelName = entityPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)[2];

                if (sourceDoc[referencedEntityType] == null)
                {
                    sourceDoc[referencedEntityType] = new JObject();
                }
                if (sourceDoc[referencedEntityType][referencedModelName] == null && !visitedEntities.Contains(referencedModelName))
                {
                    visitedEntities.Add(referencedModelName);
                    if (filePath != null)
                    {
                        //recursively check if the model is completely defined.
                        EnsureCompleteDefinitionIsPresent(visitedEntities, externalFiles, sourceFilePath, filePath, referencedEntityType, referencedModelName);
                        sourceDoc[referencedEntityType][referencedModelName] = externalFiles[filePath][referencedEntityType][referencedModelName];
                    }
                    else
                    {
                        //recursively check if the model is completely defined.
                        EnsureCompleteDefinitionIsPresent(visitedEntities, externalFiles, sourceFilePath, currentFilePath, referencedEntityType, referencedModelName);
                        sourceDoc[referencedEntityType][referencedModelName] = currentDoc[referencedEntityType][referencedModelName];
                    }

                }
            }

            //ensure that all the models that are an allOf on the current model in the external doc are also included
            if (entityType != null && modelName != null)
            {
                var reference = "#/" + entityType + "/" + modelName;
                IEnumerable<JToken> dependentRefs = currentDoc.SelectTokens("$..allOf[*].$ref").Where(r => ((string)r).Contains(reference) && !((string)r).StartsWith(reference));
                foreach (JToken dependentRef in dependentRefs)
                {
                    //the JSON Path "definitions.ModelName.allOf[0].$ref" provides the name of the model that is an allOf on the current model
                    string[] refs = dependentRef.Path.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                    if (refs[1] != null && !visitedEntities.Contains(refs[1]))
                    {
                        //recursively check if the model is completely defined.
                        EnsureCompleteDefinitionIsPresent(visitedEntities, externalFiles, sourceFilePath, currentFilePath, refs[0], refs[1]);
                        sourceDoc[refs[0]][refs[1]] = currentDoc[refs[0]][refs[1]];
                    }
                }
            }
        }
        public static string Normalize(string path, string swaggerDocument)
        {
            if (!swaggerDocument.IsYaml()) // try parse as markdown if it is not YAML
            {
                //Logger.Instance.Log(Category.Info, "Parsing as literate Swagger");
                swaggerDocument = LiterateYamlParser.Parse(swaggerDocument);
            }
            // normalize YAML to JSON since that's what we process
            //swaggerDocument = swaggerDocument.EnsureYamlIsJson();
            swaggerDocument = ResolveExternalReferencesInJson(path, swaggerDocument);
            return swaggerDocument;
        }
        public static void EnsureCompleteExampleIsPresent(HashSet<string> visitedEntities, Dictionary<string, JObject> externalFiles, string sourceFilePath)
        {
            IEnumerable<JToken> references;
            var sourceDoc = externalFiles[sourceFilePath];
            var currentDoc = externalFiles[sourceFilePath];
            references = currentDoc.SelectTokens("$..$ref").Where(p => !((string)p).StartsWith("#") && ((string)p).Contains("example"));
            while (references.Count() >0)
            {
                var value = references.ElementAt(0);
                var path = (string)value;
                string[] splitReference = path.Split(new[] { '#' }, StringSplitOptions.RemoveEmptyEntries);
                string filePath = null, entityPath = path;

                if (path.Contains("example"))
                {
                    //Fix Bug: for example:"$ref": "examples/edge-modules-delete.json"
                    if (!path.Contains("/example"))
                    {
                        path=path.Replace("example", "./example");
                    }
                    filePath = path;
                    // Make sure the filePath is either an absolute uri, or a rooted path
                    if (!Settings.FileSystem.IsCompletePath(filePath))
                    {
                        // Otherwise, root it from the directory (one level up) of the current swagger file path
                        filePath = Settings.FileSystem.MakePathRooted(Settings.FileSystem.GetParentDir(sourceFilePath), filePath);
                    }
                    if (!externalFiles.ContainsKey(filePath))
                    {
                        var externalDefinitionString = Settings.FileSystem.ReadAllText(filePath);
                        externalFiles[filePath] = JObject.Parse(externalDefinitionString);
                    }
                }

                sourceDoc.Root.SetByPath(value.Parent.Parent.Path, externalFiles[filePath]);
            }
        }
    }
}
