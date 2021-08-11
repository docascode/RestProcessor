namespace Microsoft.RestApi.SwaggerResolver.Core.Utilities.Collections
{
    using System;
    public interface ICopyFrom<in T> : ICopyFrom
    {
        bool CopyFrom(T source);
    }

    public interface ICopyFrom
    {
        bool CopyFrom(object source);
    }
}
