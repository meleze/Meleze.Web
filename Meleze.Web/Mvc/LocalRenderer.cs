using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace Meleze.Web.Mvc
{
    /// <summary>
    /// LocalRenderer executes a controller action locally.
    /// It can be used for example to generate a mail content from a MVC view.
    /// </summary>
    public sealed class LocalRenderer
    {
        public HttpContext Context { private get; set; }

        /// <summary>
        /// Renders a MVC controller action in a string.
        /// The given valueProvider provides the action parameters.
        /// </summary>
        /// <param name="routeValues"></param>
        /// <param name="resolver"></param>
        /// <returns></returns>
        public string InvokeAction(IDictionary<string, object> routeValues, IValueProvider valueProvider, Uri baseUrl)
        {
            var routeData = new RouteData();
            foreach (var routeValue in routeValues)
            {
                routeData.Values.Add(routeValue.Key, routeValue.Value);
            }
            string controllerName = (string)routeData.Values["controller"];
            string actionName = (string)routeData.Values["action"];

            var currentContext = HttpContext.Current;
            bool httpContextIsNull = currentContext == null;
            if (httpContextIsNull)
            {
                currentContext = Context;
                HttpContext.Current = Context;
            }

            var virtualPath = RouteTable.Routes.GetVirtualPath(new RequestContext(new HttpContextWrapper(currentContext), routeData), routeData.Values);

            var httpContext = new LocalHttpContext(baseUrl, virtualPath.VirtualPath, currentContext.Request.ApplicationPath, currentContext.Server, currentContext.Cache);
            var requestContext = new RequestContext();
            requestContext.RouteData = routeData;
            requestContext.HttpContext = httpContext;

            // Find the controller instance
            var controllerFactory = ControllerBuilder.Current.GetControllerFactory();
            var controller = (Controller)controllerFactory.CreateController(requestContext, controllerName);
            var actionInvoker = controller.ActionInvoker;

            // All the variables are binded to the action parameters with a special value provider
            controller.ValueProvider = valueProvider;
            var controllerContext = new ControllerContext(requestContext, controller);

            // Invoke the action and fills the response
            actionInvoker.InvokeAction(controllerContext, actionName);
            if (httpContextIsNull)
            {
                HttpContext.Current = null;
            }

            // We extract the response to build the mail
            httpContext.Response.Close();
            var bytes = ((LocalHttpResponse)httpContext.Response).stream.ToArray();
            var body = Encoding.UTF8.GetString(bytes);

            return body;
        }

        private sealed class LocalHttpRequest : HttpRequestBase
        {
            private string rawUrl;
            private string applicationPath;
            private Uri url;

            private string pathRelative;

            public LocalHttpRequest(Uri url, string rawUrl, string applicationPath)
            {
                this.url = url;
                this.rawUrl = rawUrl;
                this.applicationPath = applicationPath;

                pathRelative = rawUrl.Substring(applicationPath.Length);
                pathRelative = ((pathRelative.Length == 0) || (pathRelative[0] != '/')) ? ("~/" + pathRelative) : ('~' + pathRelative);
            }

            public override string AppRelativeCurrentExecutionFilePath { get { return pathRelative; } }

            public override string ApplicationPath { get { return applicationPath ?? string.Empty; } }

            public override System.Collections.Specialized.NameValueCollection Headers { get { return new System.Collections.Specialized.NameValueCollection(); } }

            public override string HttpMethod { get { return "GET"; } }

            public override bool IsLocal { get { return true; } }

            public override string RawUrl { get { return rawUrl ?? string.Empty; } }

            public override System.Collections.Specialized.NameValueCollection Params { get { return new System.Collections.Specialized.NameValueCollection(); } }

            public override string Path { get { return rawUrl ?? string.Empty; } }

            public override System.Collections.Specialized.NameValueCollection ServerVariables { get { return null; } }

            public override Uri Url { get { return url; } }

            public override void ValidateInput()
            {
            }
        }

        private sealed class LocalHttpResponse : HttpResponseBase
        {
            public MemoryStream stream;

            private Stream filter;
            private TextWriter writer;
            private string contentType;

            private static LocalHttpCachePolicy cachePolicy = new LocalHttpCachePolicy();

            public LocalHttpResponse()
            {
                stream = new MemoryStream();
                filter = stream;
                writer = new StreamWriter(filter, Encoding.UTF8);
            }

            public override void BinaryWrite(byte[] buffer)
            {
                filter.Write(buffer, 0, buffer.Length);
            }
            public override void Close()
            {
                writer.Close();
            }
            public override void Flush()
            {
                writer.Flush();
            }
            public override void TransmitFile(string filename)
            {
                BinaryWrite(File.ReadAllBytes(filename));
            }
            public override void TransmitFile(string filename, long offset, long length)
            {
                filter.Write(File.ReadAllBytes(filename), (int)offset, (int)length);
            }

            public override void Write(char ch)
            {
                writer.Write(ch);
            }
            public override void Write(char[] buffer, int index, int count)
            {
                writer.Write(buffer, index, count);
            }
            public override void Write(object obj)
            {
                writer.Write(obj);
            }
            public override void Write(string s)
            {
                writer.Write(s);
            }
            public override void WriteFile(IntPtr fileHandle, long offset, long size)
            {
            }
            public override void WriteFile(string filename)
            {
                TransmitFile(filename);
            }
            public override void WriteFile(string filename, bool readIntoMemory)
            {
                TransmitFile(filename);
            }
            public override void WriteFile(string filename, long offset, long size)
            {
                TransmitFile(filename, offset, size);
            }


            public override TextWriter Output { get { return writer; } set { base.Output = value; } }
            public override Stream OutputStream { get { return filter ?? stream; } }
            public override string ContentType { get { return contentType; } set { contentType = value; } }

            public override void AddCacheDependency(params System.Web.Caching.CacheDependency[] dependencies)
            {
            }
            public override void AddCacheItemDependencies(string[] cacheKeys)
            {
            }
            public override void AddCacheItemDependencies(System.Collections.ArrayList cacheKeys)
            {
            }
            public override void AddCacheItemDependency(string cacheKey)
            {
            }
            public override void AddFileDependencies(string[] filenames)
            {
            }
            public override void AddFileDependencies(System.Collections.ArrayList filenames)
            {
            }
            public override void AddFileDependency(string filename)
            {
            }
            public override void AddHeader(string name, string value)
            {
            }
            public override void AppendCookie(HttpCookie cookie)
            {
            }
            public override void AppendHeader(string name, string value)
            {
            }
            public override void AppendToLog(string param)
            {
            }
            public override string ApplyAppPathModifier(string virtualPath)
            {
                return virtualPath;
            }
            public override void Clear()
            {
            }
            public override void ClearContent()
            {
            }
            public override void ClearHeaders()
            {
            }
            public override void DisableKernelCache()
            {
            }
            public override void End()
            {
            }
            public override void Pics(string value)
            {
            }
            public override void Redirect(string url)
            {
            }
            public override void Redirect(string url, bool endResponse)
            {
            }
            public override void RedirectPermanent(string url)
            {
            }
            public override void RedirectPermanent(string url, bool endResponse)
            {
            }
            public override void RedirectToRoute(object routeValues)
            {
            }
            public override void RedirectToRoute(RouteValueDictionary routeValues)
            {
            }
            public override void RedirectToRoute(string routeName)
            {
            }
            public override void RedirectToRoute(string routeName, object routeValues)
            {
            }
            public override void RedirectToRoute(string routeName, RouteValueDictionary routeValues)
            {
            }
            public override void RedirectToRoutePermanent(object routeValues)
            {
            }
            public override void RedirectToRoutePermanent(RouteValueDictionary routeValues)
            {
            }
            public override void RedirectToRoutePermanent(string routeName)
            {
            }
            public override void RedirectToRoutePermanent(string routeName, object routeValues)
            {
            }
            public override void RedirectToRoutePermanent(string routeName, RouteValueDictionary routeValues)
            {
            }
            public override void RemoveOutputCacheItem(string path)
            {
            }
            public override void RemoveOutputCacheItem(string path, string providerName)
            {
            }
            public override void SetCookie(HttpCookie cookie)
            {
            }
            public override void WriteSubstitution(HttpResponseSubstitutionCallback callback)
            {
            }
            public override bool Buffer { get { return true; } set { } }
            public override bool BufferOutput { get { return true; } set { } }

            public override HttpCachePolicyBase Cache { get { return cachePolicy; } }

            public override string CacheControl { get { return null; } set { } }
            public override string Charset { get { return null; } set { } }
            public override Encoding ContentEncoding { get { return null; } set { } }
            public override HttpCookieCollection Cookies { get { return null; } }
            public override int Expires { get { return 0; } set { } }
            public override DateTime ExpiresAbsolute { get { return default(DateTime); } set { } }
            public override Stream Filter { get { return filter; } set { filter = value; writer.Flush(); writer = new StreamWriter(filter, Encoding.UTF8); } }
            public override System.Text.Encoding HeaderEncoding { get { return null; } set { } }
            public override System.Collections.Specialized.NameValueCollection Headers { get { return new System.Collections.Specialized.NameValueCollection(); } }
            public override bool IsClientConnected { get { return false; } }
            public override bool IsRequestBeingRedirected { get { return false; } }
            public override string RedirectLocation { get { return null; } set { } }
            public override string Status { get { return null; } set { } }
            public override int StatusCode { get { return 0; } set { } }
            public override string StatusDescription { get { return null; } set { } }
            public override int SubStatusCode { get { return 0; } set { } }
            public override bool SuppressContent { get { return false; } set { } }
            public override bool TrySkipIisCustomErrors { get { return false; } set { } }
        }

        internal sealed class LocalHttpContext : HttpContextBase
        {
            private System.Web.Caching.Cache cache;
            private HttpRequestBase request;
            private HttpResponseBase response;
            private HttpServerUtilityBase utility;

            public LocalHttpContext(Uri url, string rawUrl, string applicationPath, HttpServerUtility utility, System.Web.Caching.Cache cache)
            {
                request = new LocalHttpRequest(url, rawUrl, applicationPath);
                response = new LocalHttpResponse();
                this.cache = cache;

                this.utility = new HttpServerUtilityWrapper(utility);
            }

            public override System.Web.Caching.Cache Cache { get { return cache; } }
            public override System.Web.Profile.ProfileBase Profile { get { return null; } }
            public override HttpRequestBase Request { get { return request; } }
            public override HttpResponseBase Response { get { return response; } }
            public override HttpServerUtilityBase Server { get { return utility; } }
            public override System.Security.Principal.IPrincipal User { get { return null; } set { } }
        }

        private sealed class LocalHttpCachePolicy : HttpCachePolicyBase
        {
            public override void AddValidationCallback(HttpCacheValidateHandler handler, object data)
            {
            }
            public override void AppendCacheExtension(string extension)
            {
            }
            public override void SetAllowResponseInBrowserHistory(bool allow)
            {
            }
            public override void SetCacheability(HttpCacheability cacheability)
            {
            }
            public override void SetCacheability(HttpCacheability cacheability, string field)
            {
            }
            public override void SetETag(string etag)
            {
            }
            public override void SetETagFromFileDependencies()
            {
            }
            public override void SetExpires(DateTime date)
            {
            }
            public override void SetLastModified(DateTime date)
            {
            }
            public override void SetLastModifiedFromFileDependencies()
            {
            }
            public override void SetMaxAge(TimeSpan delta)
            {
            }
            public override void SetNoServerCaching()
            {
            }
            public override void SetNoStore()
            {
            }
            public override void SetNoTransforms()
            {
            }
            public override void SetOmitVaryStar(bool omit)
            {
            }
            public override void SetProxyMaxAge(TimeSpan delta)
            {
            }
            public override void SetRevalidation(HttpCacheRevalidation revalidation)
            {
            }
            public override void SetSlidingExpiration(bool slide)
            {
            }
            public override void SetValidUntilExpires(bool validUntilExpires)
            {
            }
            public override void SetVaryByCustom(string custom)
            {
            }
        }
    }
}
