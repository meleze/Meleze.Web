using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;

namespace Meleze.Web.Mvc
{
    /// <summary>
    /// Caches the result only if a timestamp is given in the request.
    /// => the page can be uniquely identified on the client side and kept in cache indefinitively
    /// => the server has just to change the timestamp in the generated URLs to make the client
    /// perform again the request (with the new ts)
    /// </summary>
    public sealed class TimestampedCacheAttribute : OutputCacheAttribute
    {
        private static OutputCacheAttribute noCache = new OutputCacheAttribute() { Location = OutputCacheLocation.Client, Duration = 0, VaryByParam = "none" };

        private Func<int, DateTime> _getLastModified;
        private int[] _dependenciesIndexes;

        public static Func<int, DateTime> GetLastModified { get; set; }

        static TimestampedCacheAttribute()
        {
            GetLastModified = i => DateTime.Now.ToUniversalTime();
        }

        public TimestampedCacheAttribute(int[] dependenciesIndexes)
            : this(GetLastModified, dependenciesIndexes)
        {
        }

        public TimestampedCacheAttribute(Func<int, DateTime> getLastModified, int[] dependenciesIndexes)
        {
            _getLastModified = getLastModified;
            _dependenciesIndexes = dependenciesIndexes;
        }

        public override void OnResultExecuting(ResultExecutingContext filterContext)
        {
            var httpContext = filterContext.HttpContext;
            var httpPath = httpContext.Request.Path;
            var timestamp = httpContext.Request.Params["_ts"];
            TimestampState state;
            var timestampComputed = _timestampedPaths.TryGetValue(httpPath, out state) && IsTimestampUnchanged(state.timestamps);

            // We don't change the cache when we are in a recursive call (because of
            // a bug in MVC that uses the real HttpContext instead of the mock one)
            if (!(httpContext is LocalRenderer.LocalHttpContext))
            {
                if (!timestampComputed || string.IsNullOrEmpty(timestamp) || (!state.timestampedPath.EndsWith(timestamp)))
                {
                    // The response must not be cached as there is no valid TS in the request
                    noCache.OnResultExecuting(filterContext);
                }
                else
                {
                    // The response can be cached as the TS is valid
                    base.OnResultExecuting(filterContext);
                }
            }

            if (!timestampComputed)
            {
                // Get the page dependencies
                var timestamps = new DateTime[_dependenciesIndexes.Length];
                for (int i = 0; i < _dependenciesIndexes.Length; i++)
                {
                    timestamps[i] = _getLastModified(_dependenciesIndexes[i]);
                }

                // Build a stream to compute the TS
                httpContext.Response.Filter = new HashingStream()
                {
                    _stream = httpContext.Response.Filter,
                    _path = httpPath,
                    _timestamps = timestamps,
                };
            }
        }
        private bool IsTimestampUnchanged(DateTime[] timestamps)
        {
            for (int i = 0; i < _dependenciesIndexes.Length; i++)
            {
                var timestamp = timestamps[i];
                var lastModified = _getLastModified(_dependenciesIndexes[i]);
                if (lastModified > timestamp)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Computes a hashcode uniquely identifying the strem content.
        /// This generates the unique URL for the content.
        /// </summary>
        private sealed class HashingStream : Stream
        {
            public Stream _stream;
            public string _path;
            public DateTime[] _timestamps;
            public int _hash;

            public override void Close()
            {
                base.Close();
                _timestampedPaths[_path] = new TimestampState() { timestampedPath = _path + "?_ts=" + _hash.ToString("X"), timestamps = _timestamps };
            }

            public override bool CanRead
            {
                get { return _stream.CanRead; }
            }

            public override bool CanSeek
            {
                get { return _stream.CanSeek; }
            }

            public override bool CanWrite
            {
                get { return _stream.CanWrite; }
            }

            public override void Flush()
            {
                _stream.Flush();
            }

            public override long Length
            {
                get { return _stream.Length; }
            }

            public override long Position
            {
                get
                {
                    return _stream.Position;
                }
                set
                {
                    _stream.Position = value;
                }
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return _stream.Read(buffer, offset, count);
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                return _stream.Seek(offset, origin);
            }

            public override void SetLength(long value)
            {
                _stream.SetLength(value);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                for (int i = offset; i < offset + count; i++)
                {
                    _hash += (buffer[i] << ((i % 4) * 8));
                }
                _stream.Write(buffer, offset, count);
            }
        }

        private static Dictionary<string, TimestampState> _timestampedPaths = new Dictionary<string, TimestampState>();
        private static HashSet<string> _timestampedErroredPaths = new HashSet<string>();

        private struct TimestampState
        {
            public string timestampedPath;
            public DateTime[] timestamps;
        }
        public static void InvalidatePath(string path)
        {
            _timestampedPaths.Remove(path);
        }
        public static string GetTimestampedPath(string path)
        {
            TimestampState timestampedState;
            if (_timestampedPaths.TryGetValue(path, out timestampedState))
            {
                return timestampedState.timestampedPath;
            }

            if (!_timestampedErroredPaths.Contains(path))
            {
                // This path is not yet timestamped
                // => we invoke the page to compute immediatly it's timestamp
                try
                {
                    var currentContext = HttpContext.Current;

                    var routeData = System.Web.Routing.RouteTable.Routes.GetRouteData(new TimestampedHttpContext(path, currentContext.Request.PathInfo, currentContext));
                    var renderer = new LocalRenderer();

                    renderer.InvokeAction(routeData.Values, new FormCollection(), currentContext.Request.Url);

                    if (_timestampedPaths.TryGetValue(path, out timestampedState))
                    {
                        return timestampedState.timestampedPath;
                    }
                }
                catch
                {
                    // This path will be ignored as we can't precompute it
                    _timestampedErroredPaths.Add(path);
                    throw;
                }
            }
            return path;
        }

        private sealed class TimestampedHttpContext : HttpContextWrapper
        {
            private HttpRequestBase _request;

            public TimestampedHttpContext(string path, string pathInfo, HttpContext context)
                : base(context)
            {
                _request = new TimestampledHttpRequest(path, pathInfo, context.Request);
            }

            public override HttpRequestBase Request { get { return _request; } }
        }

        private sealed class TimestampledHttpRequest : HttpRequestWrapper
        {
            private string _path;
            private string _pathInfo;

            public TimestampledHttpRequest(string path, string pathInfo, HttpRequest request)
                : base(request)
            {
                this._path = path;
                this._pathInfo = pathInfo;
            }

            public override string Path { get { return _path; } }
            public override string RawUrl { get { return _path; } }
            public override string AppRelativeCurrentExecutionFilePath
            {
                get
                {
                    var relativePath = _path;
                    if (relativePath != null)
                    {
                        var applicationPath = ApplicationPath;
                        if (applicationPath != null)
                        {
                            relativePath = _path.Substring(applicationPath.Length);
                        }
                    }
                    relativePath = ((relativePath.Length == 0) || (relativePath[0] != '/')) ? ("~/" + relativePath) : ('~' + relativePath);
                    return relativePath;
                }
            }
            public override string PathInfo { get { return _pathInfo; } }
        }
    }
}
