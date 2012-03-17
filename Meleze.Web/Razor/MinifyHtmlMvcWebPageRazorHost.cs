using System.Web.Mvc.Razor;
using System.Web.Razor.Generator;

namespace Meleze.Web.Razor
{
    /// <summary>
    /// MinimizedHtmlMvcWebPageRazorHost decorates the Razor code generator.
    /// </summary>
    public sealed class MinifyHtmlMvcWebPageRazorHost : MvcWebPageRazorHost
    {
        private MinifyHtmlMinifier _minifier;

        internal MinifyHtmlMvcWebPageRazorHost(MinifyHtmlMinifier minifier, string virtualPath, string physicalPath)
            : base(virtualPath, physicalPath)
        {
            _minifier = minifier;
        }

        public override RazorCodeGenerator DecorateCodeGenerator(RazorCodeGenerator incomingCodeGenerator)
        {
            if (!incomingCodeGenerator.Host.DesignTimeMode)
            {
                var generator = new MinifyHtmlCodeGenerator(_minifier);
                if (incomingCodeGenerator is CSharpRazorCodeGenerator)
                {
                    return new MinifyHtmlCSharpCodeGenerator(generator, incomingCodeGenerator.ClassName, incomingCodeGenerator.RootNamespaceName, incomingCodeGenerator.SourceFileName, incomingCodeGenerator.Host);
                }
                if (incomingCodeGenerator is VBRazorCodeGenerator)
                {
                    return new MinifyHtmlVBCodeGenerator(generator, incomingCodeGenerator.ClassName, incomingCodeGenerator.RootNamespaceName, incomingCodeGenerator.SourceFileName, incomingCodeGenerator.Host);
                }
            }
            return base.DecorateCodeGenerator(incomingCodeGenerator);
        }
    }
}
