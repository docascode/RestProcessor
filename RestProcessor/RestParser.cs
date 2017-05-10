namespace RestProcessor
{
    using System;
    using System.IO;

    public static class RestParser
    {
        public static void Process(string sourceRootDir, string targetRootDir, string mappingFilePath)
        {
            if (!Directory.Exists(sourceRootDir))
            {
                throw new ArgumentException($"{nameof(sourceRootDir)} '{sourceRootDir}' should exist.");
            }
            if (string.IsNullOrEmpty(targetRootDir))
            {
                throw new ArgumentException($"{nameof(targetRootDir)} should not be null or empty.");
            }
            if (!File.Exists(mappingFilePath))
            {
                throw new ArgumentException($"{nameof(mappingFilePath)} '{mappingFilePath}' should exist.");
            }

            var mapping = Utility.ReadFromFile<MappingFile>(mappingFilePath);
            if (mapping.Mapping != null)
            {
                // Obsolete mapping file
                Console.WriteLine("parsing old format mapping.json.");
                (new MappingProcessor()).Process(sourceRootDir, targetRootDir, mapping);
            }
            else
            {
                Console.WriteLine("parsing new format mapping.json.");
                var orgsMappingFile = Utility.ReadFromFile<OrgsMappingFile>(mappingFilePath);
                (new OrgsMappingProcessor()).Process(sourceRootDir, targetRootDir, orgsMappingFile);
            }
            Console.WriteLine("Done processing all swagger files");
        }
    }
}
