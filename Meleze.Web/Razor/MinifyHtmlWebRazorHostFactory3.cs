using System.Web.Mvc;
using System.Web.WebPages.Razor;

namespace Meleze.Web.Razor
{
    /// <summary>
    /// Specific code for MVC3.
    /// </summary>
    public sealed partial class MinifyHtmlWebRazorHostFactory : MvcWebRazorHostFactory
    {
        public override WebPageRazorHost CreateHost(string virtualPath, string physicalPath)
        {
            WebPageRazorHost host = base.CreateHost(virtualPath, physicalPath);
            if ((host.IsSpecialPage) || (host.DesignTimeMode))
            {
                return host;
            }
            return new MinifyHtmlMvcWebPageRazorHost(_minifier, virtualPath, physicalPath);
        }
    }
}