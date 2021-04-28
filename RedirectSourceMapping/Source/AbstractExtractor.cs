namespace RedirectSourceMapping
{
    using System.Collections.Generic;

    public abstract class AbstractExtractor<T>
    {
        protected Dictionary<string, T> keyValuePairs = new Dictionary<string, T>();
        protected string dirPath;

        protected abstract void Extract();

        public virtual Dictionary<string, T> GetPulishFileStoreInfo()
        {
            Extract();
            return keyValuePairs;
        }
    }
}
