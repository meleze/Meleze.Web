using System.Web.Mvc.Razor;
using System.Web.Razor.Generator;

namespace Meleze.Web.Razor
{
    /// <summary>
    /// MinimizedHtmlMvcWebPageRazorHost decorates the Razor code generator.
    /// </summary>
    public sealed class MinifyHtmlMvcWebPageRazorHost : MvcWebPageRazorHost
    {
        public MinifyHtmlMvcWebPageRazorHost(string virtualPath, string physicalPath)
            : base(virtualPath, physicalPath)
        {
        }

        public override RazorCodeGenerator DecorateCodeGenerator(RazorCodeGenerator incomingCodeGenerator)
        {
            if (incomingCodeGenerator is CSharpRazorCodeGenerator)
            {
                return new MinifyHtmlCodeGenerator(incomingCodeGenerator.ClassName, incomingCodeGenerator.RootNamespaceName, incomingCodeGenerator.SourceFileName, incomingCodeGenerator.Host);
            }
            return base.DecorateCodeGenerator(incomingCodeGenerator);
        }
    }
}
