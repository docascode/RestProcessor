namespace RedirectSourceMapping
{
    using RedirectSourceMapping.Model;
    using System;
    using System.Collections.Generic;

    public interface IGenerator
    {
        void Generate();
    }

    public class Generator : IGenerator
    {
        private readonly AbstractExtractor<PublishFile> legacyPublishFilesExtractor;
        private readonly AbstractExtractor<PublishFile> modernPublishFilesExtractor;
        private readonly RedirectionExtractor redirectionExtractor;

        public Generator(AbstractExtractor<PublishFile> legacyPublishFilesExtractor, AbstractExtractor<PublishFile> modernPublishFilesExtractor, RedirectionExtractor redirectionExtractor)
        {
            this.legacyPublishFilesExtractor = legacyPublishFilesExtractor;
            this.modernPublishFilesExtractor = modernPublishFilesExtractor;
            this.redirectionExtractor = redirectionExtractor;
        }
        public void Generate()
        {
            Travel();
        }
        private void Travel()
        {
            var legacyInfo = legacyPublishFilesExtractor.GetPulishFileStoreInfo();
            var modernInfo = modernPublishFilesExtractor.GetPulishFileStoreInfo();
            redirectionExtractor.Extract();

            Console.WriteLine("Total link: " + modernInfo.Count);
            Console.WriteLine("Total Rules: " + redirectionExtractor.obj.List.Count);
            foreach (var item in modernInfo)
            {
                var key = item.Key;
                if (legacyInfo.ContainsKey(item.Key))
                {
                    Console.WriteLine("No change:------------------------------");
                    Console.WriteLine("Legacy:" + legacyInfo[item.Key].ToString());
                    Console.WriteLine("Modern:" + item.ToString());
                }
                else
                {
                    var guessKey = key.Replace("-", "");
                    var prefix = Constants.SplitFolder;
                    string tempStr;
                    if (legacyInfo.ContainsKey(guessKey))
                    {
                        var instance = new Redirection()
                        {
                            Source_path = prefix + "/" + guessKey.Replace("%20", " ").Replace("yml", "md"),
                            Redirect_url = modernInfo[item.Key].PublishUrl.Replace(Constants.PublishPrefix, "").Replace(Constants.PublishSuffix, "").TrimEnd('&'),
                            Redirect_document_id = true
                        };

                        Console.WriteLine("Have change:==========================");
                        Console.WriteLine(instance.ToString());

                        redirectionExtractor.Add(instance);
                    }
                    else if (Check(legacyInfo, key, out tempStr))
                    {
                        var instance = new Redirection()
                        {
                            Source_path = prefix + "/" + tempStr.Replace("%20", " ").Replace("yml", "md"),
                            Redirect_url = modernInfo[item.Key].PublishUrl.Replace(Constants.PublishPrefix, "").Replace(Constants.PublishSuffix, "").TrimEnd('&'),
                            Redirect_document_id = true
                        };

                        Console.WriteLine("Have change:==========================");
                        Console.WriteLine(instance.ToString());

                        redirectionExtractor.Add(instance);
                    }
                }
            }
            Console.WriteLine("Total Rules: " + redirectionExtractor.obj.List.Count);
            redirectionExtractor.Serilize();
        }
        private bool Check(Dictionary<string, PublishFile> dic, string target, out string str)
        {
            bool bl = false;
            var returnValue = "";
            foreach (var key in dic.Keys)
            {
                returnValue = Check(key, target);
                if (!string.IsNullOrEmpty(returnValue))
                {
                    bl = true;
                    break;
                }
            }
            str = returnValue;
            return bl;
        }
        private string Check(string source, string target)
        {
            var sourceList = source.Split("/");
            var targetList = target.Split("/");

            var bl = true;
            if (source.Length != target.Length)
            {
                bl = false;
                return "";
            }

            for (var i = 0; i < sourceList.Length; i++)
            {
                var guessKey = targetList[i];
                var item = sourceList[i];
                if ( string.Compare(item, guessKey, true) == 0
                    || string.Compare(item, guessKey.Replace("-", ""), true) == 0
                    || string.Compare(item, guessKey.Replace("-", " "), true) == 0
                    || string.Compare(item, guessKey.Replace("-", "%20"), true) == 0
                    || string.Compare(item.Replace("%20", ""), guessKey, true) == 0
                    || string.Compare(item.Replace("%20", ""), guessKey.Replace("-", ""), true) == 0
                    || string.Compare(item.Replace("%20", ""), guessKey.Replace("-", " "), true) == 0
                    || string.Compare(item.Replace("%20", ""), guessKey.Replace("-", "%20"), true) == 0
                    || string.Compare(item.Replace(" ", ""), guessKey.Replace("-", ""), true) == 0
                    || string.Compare(item.Replace(" ", ""), guessKey.Replace("-", " "), true) == 0
                    || string.Compare(item.Replace(" ", ""), guessKey.Replace("-", "%20"), true) == 0
                    || string.Compare(item.Replace(" ", ""), guessKey, true) == 0)
                //if (item == guessKey || item == guessKey.Replace("-", "") || item.Replace(" ", "") == guessKey || item.Replace(" ", "") == guessKey.Replace("-", ""))
                {

                }
                else
                {
                    bl = false;
                    break;
                }
            }

            if (!bl)
            {
                return "";
            }

            return source;
        }
    }
}
