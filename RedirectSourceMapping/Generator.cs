namespace RedirectSourceMapping
{
    using RedirectSourceMapping.Model;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Web;

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
            RemoveModernPublishedFileMD(modernInfo);
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
                            Source_path = prefix + "/" + decode(guessKey).Replace("yml", "md"),
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
                            Source_path = prefix + "/" + decode(tempStr).Replace("yml", "md"),
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

        public static void RemoveModernPublishedFileMD(Dictionary<string, PublishFile> dict)
       {
            for (int i = 0; i < dict.Count;)
            {
                var item = dict.ElementAt(i);
                Console.WriteLine(item.Key.ToString() + "   " + item.Value.ToString());
                if (item.Key.EndsWith(".md"))
                {
                    dict.Remove(item.Key);
                }
                else
                {
                    i++;
                }
            }
        }

        public static string decode(string text)
        {
            string decodedUrl = HttpUtility.UrlDecode(text);
            return decodedUrl;
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
            if (sourceList.Length != targetList.Length)
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
                    || string.Compare(item.Replace("_", ""), guessKey.Replace("-", ""), true) == 0
                    || string.Compare(item.Replace("_", ""), guessKey.Replace("-", " "), true) == 0
                    || string.Compare(item.Replace("_", ""), guessKey.Replace("-", "%20"), true) == 0
                    || string.Compare(item.Replace(" ", ""), guessKey, true) == 0
                    || string.Compare(item.Replace("%20", "").Replace("-", ""), guessKey.Replace("-", ""), true) == 0
                    || string.Compare(item.Replace("%20", "").Replace("-", ""), guessKey.Replace("-", " "), true) == 0
                    || string.Compare(item.Replace("%20", "").Replace("-", ""), guessKey.Replace("-", "%20"), true) == 0
                    || (i== sourceList.Length-1 && HandlerRemove_tag_from_operationId(guessKey, sourceList[i-1]+item))
                    )
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

        private bool HandlerRemove_tag_from_operationId(string modernKey, string compositeKey)
        {
            if (string.Compare(modernKey.Replace("-", ""),compositeKey,true)==0
                || string.Compare(modernKey.Replace("-", " "),compositeKey,true)==0
                || string.Compare(modernKey.Replace("-", "%20"),compositeKey, true) == 0
                || string.Compare(modernKey.Replace("-", ""),compositeKey.Replace("%20", ""), true) == 0
                || string.Compare(modernKey.Replace("-", " "),compositeKey.Replace("%20", ""), true) == 0
                || string.Compare(modernKey.Replace("-", "%20"),compositeKey.Replace("%20", ""), true) == 0
                || string.Compare(modernKey,compositeKey.Replace("%20", ""), true) == 0
                || string.Compare(modernKey.Replace("-", ""),compositeKey.Replace(" ", ""), true) == 0
                || string.Compare(modernKey.Replace("-", " "),compositeKey.Replace(" ", ""), true) == 0
                || string.Compare(modernKey.Replace("-", "%20"),compositeKey.Replace(" ", ""), true) == 0
                || string.Compare(compositeKey.Replace("_", ""), modernKey.Replace("-", ""), true) == 0
                || string.Compare(compositeKey.Replace("_", ""), modernKey.Replace("-", " "), true) == 0
                || string.Compare(compositeKey.Replace("_", ""), modernKey.Replace("-", "%20"), true) == 0
                || string.Compare(modernKey,compositeKey,true)== 0)
            {
                return true;
            }
            return false;
        }

        


    }
    
   
}
