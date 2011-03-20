using System;
using System.Web.Mvc;

namespace Meleze.Web.Mvc
{
    /// <summary>
    /// Use the Last-Modified and the If-Modified-Since HTTP headers
    /// to cache an action execution.
    /// The cache is based on the last modified entity in the DB.
    /// => it can be used to cache search/read requests.
    /// </summary>
    public sealed class LastModifiedCacheAttribute : FilterAttribute, IAuthorizationFilter
    {
        private Func<int, DateTime> _getLastModified;
        private int[] _dependenciesIndexes;

        private static DateTime _serverStart;

        public static Func<int, DateTime> GetLastModified { get; set; }

        static LastModifiedCacheAttribute()
        {
            _serverStart = DateTime.Now.ToUniversalTime();
            GetLastModified = i => DateTime.Now.ToUniversalTime();
        }

        public LastModifiedCacheAttribute(int[] dependenciesIndexes)
            : this(GetLastModified, dependenciesIndexes)
        {
        }

        public LastModifiedCacheAttribute(Func<int, DateTime> getLastModified, int[] dependenciesIndexes)
        {
            _getLastModified = getLastModified;
            _dependenciesIndexes = dependenciesIndexes;
            Order = -1;
        }

        public void OnAuthorization(AuthorizationContext filterContext)
        {
            var lastModified = _serverStart;

            var modifiedSinceHeader = filterContext.HttpContext.Request.Headers["If-Modified-Since"];

            var modifiedSince = default(DateTime);
            var notModified = (modifiedSinceHeader != null) && (DateTime.TryParseExact(modifiedSinceHeader, "R", null, System.Globalization.DateTimeStyles.None, out modifiedSince)) && (lastModified <= modifiedSince);
            if (notModified)
            {
                for (int i = 0; i < _dependenciesIndexes.Length && notModified; i++)
                {
                    var ilastModified = _getLastModified(_dependenciesIndexes[i]);
                    if (ilastModified > lastModified)
                    {
                        lastModified = ilastModified;
                    }
                }
                notModified = (lastModified <= modifiedSince);
            }
            if (notModified)
            {
                // There is no need to compute the response as it has not changed
                // since the last request
                filterContext.HttpContext.Response.StatusCode = 304;
                filterContext.Result = new ContentResult();
            }
            else
            {
                filterContext.HttpContext.Response.AppendHeader("Last-Modified", lastModified.ToString("R"));
            }
        }
    }
}
