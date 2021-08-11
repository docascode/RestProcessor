﻿namespace Microsoft.RestApi.SwaggerResolver.Core.Utilities
{
    using System;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;

    public interface IFileSystem
    {
        bool IsCompletePath(string path);
        string MakePathRooted(Uri rootPath, string relativePath);
        Uri GetParentDir(string path);
        string ReadAllText(string path);
        void WriteAllText(string path, string contents);
    }
    public class FileSystem : IFileSystem
    {
        public string CurrentDirectory => Directory.GetCurrentDirectory();
        /// <summary>
        /// Returns whether or not that <paramref name="path"/> is an absolute URI or rooted path
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public bool IsCompletePath(string path)
            => Path.IsPathRooted(path) || Uri.IsWellFormedUriString(path, UriKind.Absolute);
        /// <summary>
        /// Roots the <paramref name="relativePath"/> using the <paramref name="rootPath"/>
        /// Works whether the <paramref name="rootPath"/> is an absolute URI (e.g. https://contoso.com/swaggers)
        /// or a rooted local URI (e.g. C:/swaggers/)
        /// </summary>
        /// <param name="rootPath"></param>
        /// <param name="relativePath"></param>
        /// <returns></returns>
        public string MakePathRooted(Uri rootPath, string relativePath)
        {
            //Path.Combine("/usr/foo", "./bar/baz") -> "/usr/foo/./bar/baz". Hence we need to prepend
            //file scheme to the absolute path and then use new Uri("file:///usr/foo/./bar/baz").AbsoluteUri 
            //to get "/usr/foo/bar/baz".
            var fileSchemaPrefix = "file://";
            var rootPathAsString = rootPath.ToString();
            if (rootPath != null && !Regex.IsMatch(rootPathAsString, @"^(file|https?)://.*$", RegexOptions.IgnoreCase))
            {
                //On a linux system, Path.IsPathRooted("C:/Foo") -> false. Ideally, it is not expected from 
                //someone to provide that kind of a file path while running AutoRest on a linux based system.
                //However, adding the extra condition to do the right behavior for "C:\\Foo". The focus is to 
                //do the right thing based on the initial characters. If the provided path is incorrect, it will
                //eventually fail.
                if (Path.IsPathRooted(rootPathAsString) || (Path.PathSeparator != ';' && Regex.IsMatch(rootPathAsString, @"^[a-zA-Z]:(\\{1,2}|/)\w+.*$", RegexOptions.IgnoreCase)))
                {
                    rootPathAsString = string.Concat(fileSchemaPrefix, rootPathAsString);
                }
                else
                {
                    rootPathAsString = string.Concat(fileSchemaPrefix, Path.GetFullPath(rootPathAsString));
                }
            }

            return new Uri(Path.Combine(new Uri(rootPathAsString).AbsoluteUri, relativePath), UriKind.Absolute).ToString();
        }
        public Uri GetParentDir(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                //throw new Exception(Resources.PathCannotBeNullOrEmpty);
            }
            if (IsCompletePath(path))
            {
                return new Uri(Regex.Match(path, @"^(?<dir>.*)[\\\/].*$").Groups["dir"].Value, UriKind.RelativeOrAbsolute);
            }
            else
            {
                return new Uri(Directory.GetParent(Path.Combine(Directory.GetCurrentDirectory(), path)).FullName, UriKind.Relative);
            }
        }
        public string ReadAllText(string path)
        {
            path = path.AdjustGithubUrl();

            Uri.TryCreate(path, UriKind.RelativeOrAbsolute, out Uri uri);

            if (!uri.IsAbsoluteUri)
            {
                return File.ReadAllText(Path.Combine(CurrentDirectory, path));
            }

            if (uri.IsFile)
            {
                return File.ReadAllText(uri.LocalPath, Encoding.UTF8);
            }

            using (var client = new System.Net.Http.HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "AutoRest");
                // client.Encoding = Encoding.UTF8;
                return client.GetAsync(path).Result.Content.ReadAsStringAsync().Result;
                //return client.DownloadString(path);
            }
        }
        public void WriteAllText(string path, string contents)
        {
            var eol = path.LineEnding();
            var lines = contents.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            if (File.Exists(path))
            {
                var filename = Path.GetFileNameWithoutExtension(path);
                var sourceSwagger = path.Replace($"{filename}.json",$"{filename}_sourceswagger.json");
                if (File.Exists(sourceSwagger))
                {
                    File.Delete(sourceSwagger);
                }

                File.Move(path, sourceSwagger);
            }
            // write out the file, with correct line endings for file.
            using (var writer = GetTextWriter(path))
            {
                for (var i = 0; i < lines.Length; ++i)
                {
                    if (i != 0)
                    {
                        writer.Write(eol);
                    }
                    writer.Write(lines[i]);
                }
            }
        }
        public TextWriter GetTextWriter(string path)
        {
            if (File.Exists(path))
            {
                return File.AppendText(path);
            }
            // ensure that we're being very very explicit: NO BYTE ORDER MARK. 
            return new StreamWriter(new FileStream(path, FileMode.Create), new UTF8Encoding(false, true));
        }
    }
   
}
