namespace RedirectSourceMapping
{
    using Newtonsoft.Json;
    using RedirectSourceMapping.Model;
    using System;
    using System.IO;
    public class RedirectionExtractor { 
        public Redirections obj = new Redirections();
        protected string dirPath;
        protected string fileName;
        public RedirectionExtractor(string dirPath)
        {
            this.dirPath = dirPath;
        }

        public void Add(Redirection item)
        {
            obj.List.Add(item);
        }

        public void Extract()
        {
            LoadFile();
        }

        public void Serilize()
        {
            var newdirPath= Path.Combine(Path.Combine(dirPath, "New"));
            if (!Directory.Exists(newdirPath))
            {
                Directory.CreateDirectory(newdirPath);
            }

            using (StreamWriter file = File.CreateText(Path.Combine(newdirPath, fileName)))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, obj);
            }
        }
        private void LoadFile()
        {
            var oldDirPath = Path.Combine(dirPath,"Old");
            if (string.IsNullOrEmpty(oldDirPath))
            {
                var erroeMsg = "Error: Redirection Old Path is Empty";
                Console.WriteLine(erroeMsg);
                throw new Exception(erroeMsg);
            }

            if (!Directory.Exists(oldDirPath))
            {
                var erroeMsg = string.Format("Error: Redirection Old Path {0} destn't exist", dirPath);
                Console.WriteLine(erroeMsg);
                throw new Exception(erroeMsg);
            }

            var files = Directory.GetFiles(oldDirPath);
            if (files.Length != 1)
            {
                var erroeMsg = "Error: Redirection in old Path can only have one file, not empty or multiple files";
                Console.WriteLine(erroeMsg);
                throw new Exception(erroeMsg);
            }
            fileName = Path.GetFileName(files[0]);
            ReadFile(files[0]);
        }

        private void ReadFile(string filePath)
        {
            using (StreamReader file = File.OpenText(filePath))
            {
                JsonSerializer serializer = new JsonSerializer();
                obj = (Redirections)serializer.Deserialize(file, typeof(Redirections));
                if (obj.List == null)
                {
                    obj.List = new System.Collections.Generic.HashSet<Redirection>();
                }
            }
        }
    }
}
