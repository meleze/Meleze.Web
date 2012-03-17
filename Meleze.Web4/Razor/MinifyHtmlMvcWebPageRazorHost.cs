using System.Web.Mvc.Razor;
using System.Web.Razor.Parser;
using System.Web.WebPages.Razor;

namespace Meleze.Web.Razor
{
    /// <summary>
    /// MinimizedHtmlMvcWebPageRazorHost decorates the Razor code generator.
    /// </summary>
    public sealed class MinifyHtmlMvcWebPageRazorHost : MvcWebPageRazorHost
    {
        private MinifyHtmlMinifier _minifier;
        private WebPageRazorHost _host;

        internal MinifyHtmlMvcWebPageRazorHost(WebPageRazorHost host, MinifyHtmlMinifier minifier, string virtualPath, string physicalPath)
            : base(virtualPath, physicalPath)
        {
            _host = host;
            _minifier = minifier;
        }

        public override ParserBase DecorateMarkupParser(ParserBase incomingMarkupParser)
        {
            var parser = base.DecorateMarkupParser(incomingMarkupParser);
            if (!(parser is HtmlMarkupParser))
            {
                // That's not HTML => we don't know how to minify it
                return parser;
            }

            var newparser = new MinifyHtmlParser(parser, _minifier);
            return newparser;
        }
    }
}
