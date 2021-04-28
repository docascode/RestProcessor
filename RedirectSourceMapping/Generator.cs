namespace RedirectSourceMapping
{
    using RedirectSourceMapping.Model;
    using System;

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

            foreach (var item in modernInfo)
            {
                var key = item.Key;
                if (legacyInfo.ContainsKey(item.Key))
                {
                    Console.WriteLine("No change:------------------------------");
                    Console.WriteLine("Legacy:" + legacyInfo[item.Key].ToString());
                    Console.WriteLine("Modern:" + legacyInfo[item.Key].ToString());
                }
                else
                {
                    var guessKey = key.Replace("-", "");

                    if (legacyInfo.ContainsKey(guessKey))
                    {
                        var instance = new Redirection()
                        {
                            Source_path = Constants.SplitFolder + "/" + guessKey.Replace("yml", "md"),
                            Redirect_url = modernInfo[item.Key].PublishUrl.Replace(Constants.PublishPrefix, "").Replace(Constants.PublishSuffix, ""),
                            Redirect_document_id = true
                        };

                        Console.WriteLine("Have change:==========================");
                        Console.WriteLine(instance.ToString());

                        redirectionExtractor.Add(instance);
                    }
                }
            }

            redirectionExtractor.Serilize();
        }
    }
}
