using System.Web.Mvc;
using System.Web.WebPages.Razor;

namespace Meleze.Web.Razor
{
    /// <summary>
    /// MinifyHtmlWebRazorHostFactory removes useless whitespace and comments
    /// in an HTML page.
    /// It is executed when Razor generate the code for a page, just before the compilation.
    /// => There is a small performance penalty when the page compiles and a small optimization at execution.
    /// </summary>
    public sealed class MinifyHtmlWebRazorHostFactory : MvcWebRazorHostFactory
    {
        public override WebPageRazorHost CreateHost(string virtualPath, string physicalPath)
        {
            WebPageRazorHost host = base.CreateHost(virtualPath, physicalPath);
            if ((host.IsSpecialPage) || (host.DesignTimeMode))
            {
                return host;
            }
            return new MinifyHtmlMvcWebPageRazorHost(virtualPath, physicalPath);
        }
    }
}