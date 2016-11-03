namespace RestProcessor
{
    using System;
    using System.IO;

    public static class FileUtility
    {
        public static string GetDirectoryName(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException($"{nameof(path)} should not be null or empty");
            }

            return new DirectoryInfo(path).Name;
        }

        public static string CreateDirectoryIfNotExist(string directory)
        {
            if (string.IsNullOrEmpty(directory))
            {
                throw new ArgumentException($"{nameof(directory)} should not be null or empty");
            }

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            return directory;
        }

        public static string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }
            return path.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        public static string GetRelativePath(string path, string basePath)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException($"{nameof(path)} should not be null or empty");
            }
            if (string.IsNullOrEmpty(basePath))
            {
                throw new ArgumentException($"{nameof(basePath)} should not be null or empty");
            }

            var pathUri = new Uri(path);
            var basePathUri = new Uri(basePath);
            var relativePathToBaseUri = basePathUri.MakeRelativeUri(pathUri);
            return relativePathToBaseUri.OriginalString.Length == 0 ? Path.GetFileName(path) : relativePathToBaseUri.OriginalString;
        }
    }
}
