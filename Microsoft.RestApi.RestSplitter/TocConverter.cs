namespace Microsoft.RestApi
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using Microsoft.DocAsCode.Build.TableOfContents;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.Common;

    internal static class TocConverter
    {
        private static readonly string YmlExtension = ".yml";

        public static string Convert(string tocMarkdownFilePath, string tocYmlPath = null)
        {
            if (string.IsNullOrEmpty(tocMarkdownFilePath))
            {
                throw new ArgumentException($"{nameof(tocMarkdownFilePath)} can't be null or empty");
            }

            if (!File.Exists(tocMarkdownFilePath))
            {
                throw new FileNotFoundException($"{tocMarkdownFilePath} can't be found.");
            }

            tocYmlPath = tocYmlPath ?? Path.ChangeExtension(tocMarkdownFilePath, YmlExtension);
            ConvertCore(tocMarkdownFilePath, tocYmlPath);

            return tocYmlPath;
        }
        public static void GenerateOverviewNode(string tocMarkdownFilePath)
        {
            TocViewModel tocModel;
            using (var sr = new StreamReader(tocMarkdownFilePath))
            {
                tocModel = MarkdownTocReader.LoadToc(sr.ReadToEnd(), tocMarkdownFilePath);
            }

            if (tocModel == null) return;
            var tocNodeQueue = new Queue<TocItemViewModel>();
            tocModel.ForEach(item => tocNodeQueue.Enqueue(item));
            while (tocNodeQueue.Count > 0)
            {
                var tocNode = tocNodeQueue.Dequeue();
                if (tocNode.Items != null)
                {
                    tocNode.Items.ForEach(item => tocNodeQueue.Enqueue(item));

                    if (tocNode.Href != null)
                    {
                        tocNode.Items.Insert(0, new TocItemViewModel
                        {
                            Name = "Overview",
                            Href = tocNode.Href
                        });
                        tocNode.Href = null;
                    }
                }
            }

            using (var writer = new StreamWriter(tocMarkdownFilePath))
            {
                WriteTocModelToFile(tocModel, writer, 1);
            }
        }

        private static void ConvertCore(string tocMarkdownFilePath, string tocYmlPath)
        {
            using (var sr = new StreamReader(tocMarkdownFilePath))
            {
                var tocModel = MarkdownTocReader.LoadToc(sr.ReadToEnd(), tocMarkdownFilePath);
                YamlUtility.Serialize(tocYmlPath, tocModel);
            }
        }

        private static void WriteTocModelToFile(TocViewModel tocModel, StreamWriter writer, int level)
        {
            if (tocModel == null) return;
            foreach (var item in tocModel)
            {
                if (!string.IsNullOrEmpty(item.Href))
                {
                    writer.WriteLine($"{new string('#', level)} [{item.Name}]({item.Href})");
                }
                else
                {
                    writer.WriteLine($"{new string('#', level)} {item.Name}");
                }
                WriteTocModelToFile(item.Items, writer, level + 1);
            }
        }
    }
}
